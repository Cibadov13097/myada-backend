namespace CallService.Api.Realtime;

public interface ICallStateStore
{
    Task UpsertConnectionAsync(ConnectedUserConnection connection, CancellationToken cancellationToken = default);
    Task<ConnectedUserConnection?> GetConnectionAsync(string connectionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConnectedUserConnection>> GetUserConnectionsAsync(string userId, CancellationToken cancellationToken = default);
    Task<DisconnectedConnectionState?> RemoveConnectionAsync(string connectionId, CancellationToken cancellationToken = default);

    Task<string?> GetRoomIdForConnectionAsync(string connectionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConnectedUserConnection>> GetRoomParticipantsAsync(string roomId, CancellationToken cancellationToken = default);
    Task AddConnectionToRoomAsync(string roomId, string connectionId, CancellationToken cancellationToken = default);
    Task<string?> RemoveConnectionFromRoomAsync(string connectionId, CancellationToken cancellationToken = default);

    Task<CallRequestState> CreatePendingCallAsync(CallRequestState pendingCall, CancellationToken cancellationToken = default);
    Task<CallRequestState?> GetCallAsync(string callId, CancellationToken cancellationToken = default);
    Task<CallRequestState?> TryTransitionCallAsync(
        string callId,
        CallRequestStatus expectedStatus,
        CallRequestStatus newStatus,
        string resolvedByUserId,
        string? resolvedByConnectionId,
        string? reason,
        DateTime resolvedAtUtc,
        CancellationToken cancellationToken = default);
}
