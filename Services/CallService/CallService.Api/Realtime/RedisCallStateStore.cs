using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CallService.Api.Realtime;

public sealed class RedisCallStateStore : ICallStateStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly IDatabase _database;
    private readonly TimeSpan _stateTtl;

    public RedisCallStateStore(IConnectionMultiplexer redis, IOptions<CallRuntimeOptions> options)
    {
        _database = redis.GetDatabase();

        var configuredHours = options.Value.StateTtlHours;
        var ttlHours = configuredHours <= 0 ? 24 : configuredHours;
        _stateTtl = TimeSpan.FromHours(ttlHours);
    }

    public async Task UpsertConnectionAsync(ConnectedUserConnection connection, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(connection.ConnectionId))
            throw new ArgumentException("ConnectionId is required.", nameof(connection));

        if (string.IsNullOrWhiteSpace(connection.UserId))
            throw new ArgumentException("UserId is required.", nameof(connection));

        await _database.StringSetAsync(ConnectionKey(connection.ConnectionId), Serialize(connection), _stateTtl);
        await _database.SetAddAsync(UserConnectionsKey(connection.UserId), connection.ConnectionId);
        await _database.KeyExpireAsync(UserConnectionsKey(connection.UserId), _stateTtl);
    }

    public async Task<ConnectedUserConnection?> GetConnectionAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(connectionId))
            return null;

        var raw = await _database.StringGetAsync(ConnectionKey(connectionId));
        return Deserialize<ConnectedUserConnection>(raw);
    }

    public async Task<IReadOnlyList<ConnectedUserConnection>> GetUserConnectionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(userId))
            return Array.Empty<ConnectedUserConnection>();

        var key = UserConnectionsKey(userId);
        var connectionIds = await _database.SetMembersAsync(key);
        if (connectionIds.Length == 0)
            return Array.Empty<ConnectedUserConnection>();

        var result = new List<ConnectedUserConnection>(connectionIds.Length);
        foreach (var connectionId in connectionIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var connection = await GetConnectionAsync(connectionId.ToString(), cancellationToken);
            if (connection is null)
            {
                await _database.SetRemoveAsync(key, connectionId);
                continue;
            }

            result.Add(connection);
        }

        return result;
    }

    public async Task<DisconnectedConnectionState?> RemoveConnectionAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(connectionId))
            return null;

        var connection = await GetConnectionAsync(connectionId, cancellationToken);
        if (connection is null)
        {
            await RemoveConnectionFromRoomAsync(connectionId, cancellationToken);
            await _database.KeyDeleteAsync(ConnectionKey(connectionId));
            return null;
        }

        var roomId = await RemoveConnectionFromRoomAsync(connectionId, cancellationToken);

        await _database.KeyDeleteAsync(ConnectionKey(connectionId));
        await _database.SetRemoveAsync(UserConnectionsKey(connection.UserId), connection.ConnectionId);

        return new DisconnectedConnectionState
        {
            Connection = connection,
            RoomId = roomId
        };
    }

    public async Task<string?> GetRoomIdForConnectionAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(connectionId))
            return null;

        var roomId = await _database.StringGetAsync(ConnectionRoomKey(connectionId));
        return roomId.IsNullOrEmpty ? null : roomId.ToString();
    }

    public async Task<IReadOnlyList<ConnectedUserConnection>> GetRoomParticipantsAsync(string roomId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(roomId))
            return Array.Empty<ConnectedUserConnection>();

        var key = RoomConnectionsKey(roomId);
        var connectionIds = await _database.SetMembersAsync(key);
        if (connectionIds.Length == 0)
            return Array.Empty<ConnectedUserConnection>();

        var participants = new List<ConnectedUserConnection>(connectionIds.Length);
        foreach (var connectionId in connectionIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var connection = await GetConnectionAsync(connectionId.ToString(), cancellationToken);
            if (connection is null)
            {
                await _database.SetRemoveAsync(key, connectionId);
                continue;
            }

            participants.Add(connection);
        }

        return participants;
    }

    public async Task AddConnectionToRoomAsync(string roomId, string connectionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(roomId))
            throw new ArgumentException("RoomId is required.", nameof(roomId));

        if (string.IsNullOrWhiteSpace(connectionId))
            throw new ArgumentException("ConnectionId is required.", nameof(connectionId));

        await RemoveConnectionFromRoomAsync(connectionId, cancellationToken);

        await _database.StringSetAsync(ConnectionRoomKey(connectionId), roomId, _stateTtl);
        await _database.SetAddAsync(RoomConnectionsKey(roomId), connectionId);
        await _database.KeyExpireAsync(RoomConnectionsKey(roomId), _stateTtl);
    }

    public async Task<string?> RemoveConnectionFromRoomAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(connectionId))
            return null;

        var roomIdValue = await _database.StringGetAsync(ConnectionRoomKey(connectionId));
        if (roomIdValue.IsNullOrEmpty)
            return null;

        var roomId = roomIdValue.ToString();
        await _database.KeyDeleteAsync(ConnectionRoomKey(connectionId));

        var roomSetKey = RoomConnectionsKey(roomId);
        await _database.SetRemoveAsync(roomSetKey, connectionId);

        if (await _database.SetLengthAsync(roomSetKey) == 0)
            await _database.KeyDeleteAsync(roomSetKey);

        return roomId;
    }

    public async Task<CallRequestState> CreatePendingCallAsync(CallRequestState pendingCall, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var created = await _database.StringSetAsync(
            CallRequestKey(pendingCall.CallId),
            Serialize(pendingCall),
            _stateTtl,
            When.NotExists);

        if (!created)
            throw new InvalidOperationException("Failed to create pending call state.");

        return pendingCall;
    }

    public async Task<CallRequestState?> GetCallAsync(string callId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(callId))
            return null;

        var value = await _database.StringGetAsync(CallRequestKey(callId));
        return Deserialize<CallRequestState>(value);
    }

    public async Task<CallRequestState?> TryTransitionCallAsync(
        string callId,
        CallRequestStatus expectedStatus,
        CallRequestStatus newStatus,
        string resolvedByUserId,
        string? resolvedByConnectionId,
        string? reason,
        DateTime resolvedAtUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(callId))
            return null;

        var lockKey = CallTransitionLockKey(callId);
        var lockToken = Guid.NewGuid().ToString("N");
        var lockTaken = await _database.LockTakeAsync(lockKey, lockToken, TimeSpan.FromSeconds(5));
        if (!lockTaken)
            return null;

        try
        {
            var current = await GetCallAsync(callId, cancellationToken);
            if (current is null || current.Status != expectedStatus)
                return null;

            current.Status = newStatus;
            current.ResolvedAtUtc = resolvedAtUtc;
            current.ResolvedByUserId = resolvedByUserId;
            current.ResolvedByConnectionId = resolvedByConnectionId;
            current.ResolveReason = reason;

            await _database.StringSetAsync(CallRequestKey(callId), Serialize(current), _stateTtl);
            return current;
        }
        finally
        {
            await _database.LockReleaseAsync(lockKey, lockToken);
        }
    }

    private static RedisValue Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, SerializerOptions);
    }

    private static T? Deserialize<T>(RedisValue value) where T : class
    {
        if (value.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<T>(value.ToString(), SerializerOptions);
    }

    private static string ConnectionKey(string connectionId) => $"call:presence:connection:{connectionId}";
    private static string UserConnectionsKey(string userId) => $"call:presence:user:{userId}:connections";
    private static string ConnectionRoomKey(string connectionId) => $"call:room:connection:{connectionId}";
    private static string RoomConnectionsKey(string roomId) => $"call:room:{roomId}:connections";
    private static string CallRequestKey(string callId) => $"call:request:{callId}";
    private static string CallTransitionLockKey(string callId) => $"call:request:{callId}:lock";
}
