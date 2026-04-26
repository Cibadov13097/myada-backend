using System.Security.Claims;
using System.Text.Json;
using CallService.Api.Persistence;
using CallService.Api.Realtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace CallService.Api.Hubs;

[Authorize]
public class CallHub : Hub
{
    private const string DispatcherRole = "dispatcher";
    private const string StudentRole = "student";
    private const string DefaultDispatcherInboxPrefix = "dispatcher-inbox-";

    private readonly ICallStateStore _stateStore;
    private readonly ICallHistoryService _callHistoryService;
    private readonly IHubContext<CallHub> _hubContext;
    private readonly ILogger<CallHub> _logger;
    private readonly CallRuntimeOptions _options;
    private readonly IIceConfigurationProvider _iceConfigurationProvider;

    public CallHub(
        ICallStateStore stateStore,
        ICallHistoryService callHistoryService,
        IHubContext<CallHub> hubContext,
        IIceConfigurationProvider iceConfigurationProvider,
        IOptions<CallRuntimeOptions> options,
        ILogger<CallHub> logger)
    {
        _stateStore = stateStore;
        _callHistoryService = callHistoryService;
        _hubContext = hubContext;
        _iceConfigurationProvider = iceConfigurationProvider;
        _logger = logger;
        _options = options.Value;
    }

    public override async Task OnConnectedAsync()
    {
        var connection = BuildCurrentConnection();
        await _stateStore.UpsertConnectionAsync(connection);
        var dispatcherInboxGroup = connection.IsDispatcher
            ? GetDispatcherInboxGroup(connection.UserId)
            : null;

        if (!string.IsNullOrWhiteSpace(dispatcherInboxGroup))
            await Groups.AddToGroupAsync(connection.ConnectionId, dispatcherInboxGroup);

        var roles = connection.Roles;
        var iceConfiguration = _iceConfigurationProvider.GetForUser(connection.UserId);

        _logger.LogInformation(
            "Connection established. ConnectionId={ConnectionId}, UserId={UserId}, IsDispatcher={IsDispatcher}",
            connection.ConnectionId,
            connection.UserId,
            connection.IsDispatcher);

        await Clients.Caller.SendAsync("Connected", new
        {
            connectionId = connection.ConnectionId,
            userId = connection.UserId,
            displayName = connection.DisplayName,
            isDispatcher = connection.IsDispatcher,
            dispatcherInboxGroup,
            roles,
            iceConfiguration
        });

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var disconnected = await _stateStore.RemoveConnectionAsync(Context.ConnectionId);

        if (disconnected?.Connection is not null)
        {
            _logger.LogInformation(
                "Connection disconnected. ConnectionId={ConnectionId}, UserId={UserId}, RoomId={RoomId}",
                disconnected.Connection.ConnectionId,
                disconnected.Connection.UserId,
                disconnected.RoomId ?? "<none>");
        }

        if (disconnected?.Connection.IsDispatcher == true)
        {
            var dispatcherInboxGroup = GetDispatcherInboxGroup(disconnected.Connection.UserId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, dispatcherInboxGroup);
        }

        if (!string.IsNullOrWhiteSpace(disconnected?.RoomId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, disconnected.RoomId);
            await Clients.OthersInGroup(disconnected.RoomId).SendAsync(
                "ParticipantLeft",
                new ParticipantDto(
                    disconnected.Connection.ConnectionId,
                    disconnected.Connection.UserId,
                    disconnected.Connection.DisplayName));

            await TryFinalizeAcceptedCallAfterDepartureAsync(
                disconnected.RoomId,
                disconnected.Connection.UserId,
                disconnected.Connection.ConnectionId,
                "Call ended because a participant disconnected.");
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task RequestCall(string targetUserId)
    {
        if (string.IsNullOrWhiteSpace(targetUserId))
            throw new HubException("targetUserId is required.");

        var caller = await GetCurrentConnectionOrThrowAsync();
        targetUserId = targetUserId.Trim();

        _logger.LogInformation(
            "Call request created by caller {CallerUserId} ({CallerConnectionId}) targeting user {TargetUserId}",
            caller.UserId,
            caller.ConnectionId,
            targetUserId);

        if (string.Equals(caller.UserId, targetUserId, StringComparison.Ordinal))
            throw new HubException("You cannot call yourself.");

        var targetConnections = await _stateStore.GetUserConnectionsAsync(targetUserId);
        if (targetConnections.Count == 0)
            throw new HubException("Target user is offline.");

        ValidateCallTarget(caller, targetConnections);

        var targetDisplayName = targetConnections[0].DisplayName;

        var nowUtc = DateTime.UtcNow;
        var timeout = TimeSpan.FromSeconds(GetInviteTimeoutSeconds());
        var expiresAtUtc = nowUtc.Add(timeout);
        var callId = Guid.NewGuid().ToString("N");
        var roomId = $"call-{callId}";

        var pendingCall = new CallRequestState
        {
            CallId = callId,
            RoomId = roomId,
            CallerUserId = caller.UserId,
            CallerConnectionId = caller.ConnectionId,
            CallerDisplayName = caller.DisplayName,
            DispatcherUserId = targetUserId,
            Status = CallRequestStatus.Pending,
            RequestedAtUtc = nowUtc,
            ExpiresAtUtc = expiresAtUtc
        };

        await _stateStore.CreatePendingCallAsync(pendingCall);
        await _callHistoryService.CreatePendingCallAsync(
            pendingCall,
            targetDisplayName,
            CancellationToken.None);

        _logger.LogInformation(
            "Pending call state saved. CallId={CallId}, RoomId={RoomId}, Caller={CallerUserId}, Target={TargetUserId}, ExpiresAtUtc={ExpiresAtUtc:o}",
            pendingCall.CallId,
            pendingCall.RoomId,
            pendingCall.CallerUserId,
            pendingCall.DispatcherUserId,
            pendingCall.ExpiresAtUtc);

        var incomingPayload = new IncomingCallDto(
            pendingCall.CallId,
            pendingCall.RoomId,
            pendingCall.CallerUserId,
            pendingCall.CallerConnectionId,
            pendingCall.CallerDisplayName,
            pendingCall.RequestedAtUtc,
            pendingCall.ExpiresAtUtc);

        await _hubContext.Clients
            .User(targetUserId)
            .SendAsync("IncomingCall", incomingPayload);

        await _hubContext.Clients.User(caller.UserId).SendAsync(
            "CallRinging",
            new CallRingingDto(
                pendingCall.CallId,
                pendingCall.RoomId,
                pendingCall.DispatcherUserId,
                pendingCall.DispatcherUserId,
                pendingCall.RequestedAtUtc,
                pendingCall.ExpiresAtUtc));

        _ = ScheduleTimeoutAsync(pendingCall.CallId, timeout);
    }

    public async Task AcceptCall(string callId)
    {
        if (string.IsNullOrWhiteSpace(callId))
            throw new HubException("callId is required.");

        var callee = await GetCurrentConnectionOrThrowAsync();

        var existingCall = await _stateStore.GetCallAsync(callId);
        if (existingCall is null)
            throw new HubException("Call request not found.");

        if (!string.Equals(existingCall.DispatcherUserId, callee.UserId, StringComparison.Ordinal))
            throw new HubException("This call was not requested for the current user.");

        if (existingCall.Status != CallRequestStatus.Pending)
            throw new HubException($"Call request is already {ToClientStatus(existingCall.Status)}.");

        if (existingCall.ExpiresAtUtc <= DateTime.UtcNow)
        {
            var timedOut = await _stateStore.TryTransitionCallAsync(
                existingCall.CallId,
                CallRequestStatus.Pending,
                CallRequestStatus.TimedOut,
                "system",
                null,
                "Invite expired.",
                DateTime.UtcNow);

            if (timedOut is not null)
            {
                await _callHistoryService.UpdateCallStatusAsync(
                    timedOut,
                    callee.DisplayName,
                    CancellationToken.None);
                await BroadcastTimedOutAsync(timedOut);
            }

            throw new HubException("Call request expired.");
        }

        var accepted = await _stateStore.TryTransitionCallAsync(
            existingCall.CallId,
            CallRequestStatus.Pending,
            CallRequestStatus.Accepted,
            callee.UserId,
            callee.ConnectionId,
            null,
            DateTime.UtcNow);

        if (accepted is null)
            throw new HubException("Call request state changed. Please refresh and try again.");

        await _callHistoryService.UpdateCallStatusAsync(
            accepted,
            callee.DisplayName,
            CancellationToken.None);

        var callerConnection = await _stateStore.GetConnectionAsync(accepted.CallerConnectionId);
        if (callerConnection is null)
        {
            callerConnection = (await _stateStore.GetUserConnectionsAsync(accepted.CallerUserId))
                .FirstOrDefault();
        }

        if (callerConnection is null)
        {
            var cancelled = await _stateStore.TryTransitionCallAsync(
                accepted.CallId,
                CallRequestStatus.Accepted,
                CallRequestStatus.Cancelled,
                "system",
                null,
                "Caller is no longer online.",
                DateTime.UtcNow);

            if (cancelled is not null)
            {
                await _callHistoryService.UpdateCallStatusAsync(
                    cancelled,
                    callee.DisplayName,
                    CancellationToken.None);
                await BroadcastCancelledAsync(cancelled);
            }

            throw new HubException("Caller is no longer online.");
        }

        await JoinConnectionToRoomAsync(callerConnection.ConnectionId, accepted.RoomId);
        await JoinConnectionToRoomAsync(callee.ConnectionId, accepted.RoomId);

        await NotifyJoinedRoomAsync(accepted.RoomId);

        var participants = await GetParticipantDtosAsync(accepted.RoomId);
        var acceptedAtUtc = accepted.ResolvedAtUtc ?? DateTime.UtcNow;

        _logger.LogInformation(
            "Call accepted. CallId={CallId}, RoomId={RoomId}, CalleeConnectionId={CalleeConnectionId}, CallerConnectionId={CallerConnectionId}, ParticipantCount={ParticipantCount}",
            accepted.CallId,
            accepted.RoomId,
            callee.ConnectionId,
            callerConnection.ConnectionId,
            participants.Length);

        var payload = new CallAcceptedDto(
            accepted.CallId,
            accepted.RoomId,
            callee.UserId,
            callee.ConnectionId,
            acceptedAtUtc,
            participants);

        await _hubContext.Clients.User(accepted.CallerUserId).SendAsync("CallAccepted", payload);
        await _hubContext.Clients.User(accepted.DispatcherUserId).SendAsync("CallAccepted", payload);
    }

    public async Task RejectCall(string callId, string? reason = null)
    {
        if (string.IsNullOrWhiteSpace(callId))
            throw new HubException("callId is required.");

        var callee = await GetCurrentConnectionOrThrowAsync();

        var existingCall = await _stateStore.GetCallAsync(callId);
        if (existingCall is null)
            throw new HubException("Call request not found.");

        if (!string.Equals(existingCall.DispatcherUserId, callee.UserId, StringComparison.Ordinal))
            throw new HubException("This call was not requested for the current user.");

        var rejected = await _stateStore.TryTransitionCallAsync(
            existingCall.CallId,
            CallRequestStatus.Pending,
            CallRequestStatus.Rejected,
            callee.UserId,
            callee.ConnectionId,
            NormalizeReason(reason),
            DateTime.UtcNow);

        if (rejected is null)
            throw new HubException("Call request state changed. Please refresh and try again.");

        await _callHistoryService.UpdateCallStatusAsync(
            rejected,
            callee.DisplayName,
            CancellationToken.None);
        await BroadcastRejectedAsync(rejected);
    }

    public async Task CancelCall(string callId, string? reason = null)
    {
        if (string.IsNullOrWhiteSpace(callId))
            throw new HubException("callId is required.");

        var caller = await GetCurrentConnectionOrThrowAsync();
        var existingCall = await _stateStore.GetCallAsync(callId);
        if (existingCall is null)
            throw new HubException("Call request not found.");

        if (!string.Equals(existingCall.CallerUserId, caller.UserId, StringComparison.Ordinal))
            throw new HubException("Only the caller can cancel this call.");

        var cancelled = await _stateStore.TryTransitionCallAsync(
            existingCall.CallId,
            CallRequestStatus.Pending,
            CallRequestStatus.Cancelled,
            caller.UserId,
            caller.ConnectionId,
            NormalizeReason(reason),
            DateTime.UtcNow);

        if (cancelled is null)
            throw new HubException("Call request state changed. Please refresh and try again.");

        await _callHistoryService.UpdateCallStatusAsync(
            cancelled,
            await GetTargetDisplayNameAsync(cancelled.DispatcherUserId),
            CancellationToken.None);
        await BroadcastCancelledAsync(cancelled);
    }

    public async Task LeaveCall()
    {
        var connection = await GetCurrentConnectionOrThrowAsync();
        var roomId = await _stateStore.RemoveConnectionFromRoomAsync(connection.ConnectionId);
        if (string.IsNullOrWhiteSpace(roomId))
            return;

        await Groups.RemoveFromGroupAsync(connection.ConnectionId, roomId);

        await Clients.Caller.SendAsync("LeftRoom", new
        {
            roomId,
            connectionId = connection.ConnectionId
        });

        await Clients.OthersInGroup(roomId).SendAsync(
            "ParticipantLeft",
            new ParticipantDto(connection.ConnectionId, connection.UserId, connection.DisplayName));

        await TryFinalizeAcceptedCallAfterDepartureAsync(
            roomId,
            connection.UserId,
            connection.ConnectionId,
            "Call ended because a participant left.");
    }

    public async Task SendOffer(string targetConnectionId, string sdp)
    {
        if (string.IsNullOrWhiteSpace(targetConnectionId))
            throw new HubException("targetConnectionId is required.");

        if (string.IsNullOrWhiteSpace(sdp))
            throw new HubException("sdp is required.");

        var sender = await GetCurrentConnectionOrThrowAsync();
        var roomId = await GetCurrentRoomOrThrowAsync(sender.ConnectionId);
        await EnsureTargetInSameRoomAsync(targetConnectionId, roomId);

        _logger.LogInformation(
            "Forwarding offer. RoomId={RoomId}, FromConnectionId={FromConnectionId}, ToConnectionId={ToConnectionId}",
            roomId,
            sender.ConnectionId,
            targetConnectionId);

        await _hubContext.Clients.Client(targetConnectionId).SendAsync(
            "ReceiveOffer",
            new SignalDescriptionDto(roomId, sender.ConnectionId, sender.UserId, sdp));
    }

    public async Task SendAnswer(string targetConnectionId, string sdp)
    {
        if (string.IsNullOrWhiteSpace(targetConnectionId))
            throw new HubException("targetConnectionId is required.");

        if (string.IsNullOrWhiteSpace(sdp))
            throw new HubException("sdp is required.");

        var sender = await GetCurrentConnectionOrThrowAsync();
        var roomId = await GetCurrentRoomOrThrowAsync(sender.ConnectionId);
        await EnsureTargetInSameRoomAsync(targetConnectionId, roomId);

        _logger.LogInformation(
            "Forwarding answer. RoomId={RoomId}, FromConnectionId={FromConnectionId}, ToConnectionId={ToConnectionId}",
            roomId,
            sender.ConnectionId,
            targetConnectionId);

        await _hubContext.Clients.Client(targetConnectionId).SendAsync(
            "ReceiveAnswer",
            new SignalDescriptionDto(roomId, sender.ConnectionId, sender.UserId, sdp));
    }

    public async Task SendIceCandidate(
        string targetConnectionId,
        string candidate,
        string? sdpMid,
        int? sdpMLineIndex)
    {
        if (string.IsNullOrWhiteSpace(targetConnectionId))
            throw new HubException("targetConnectionId is required.");

        if (string.IsNullOrWhiteSpace(candidate))
            throw new HubException("candidate is required.");

        var sender = await GetCurrentConnectionOrThrowAsync();
        var roomId = await GetCurrentRoomOrThrowAsync(sender.ConnectionId);
        await EnsureTargetInSameRoomAsync(targetConnectionId, roomId);

        _logger.LogDebug(
            "Forwarding ICE candidate. RoomId={RoomId}, FromConnectionId={FromConnectionId}, ToConnectionId={ToConnectionId}, CandidateLength={CandidateLength}, SdpMid={SdpMid}, SdpMLineIndex={SdpMLineIndex}",
            roomId,
            sender.ConnectionId,
            targetConnectionId,
            candidate.Length,
            sdpMid ?? "<null>",
            sdpMLineIndex);

        await _hubContext.Clients.Client(targetConnectionId).SendAsync(
            "ReceiveIceCandidate",
            new IceCandidateDto(
                roomId,
                sender.ConnectionId,
                sender.UserId,
                candidate,
                sdpMid,
                sdpMLineIndex));
    }

    public async Task EndCall()
    {
        var sender = await GetCurrentConnectionOrThrowAsync();
        var roomId = await GetCurrentRoomOrThrowAsync(sender.ConnectionId);
        var participants = await _stateStore.GetRoomParticipantsAsync(roomId);
        var endedAtUtc = DateTime.UtcNow;

        _logger.LogInformation(
            "Ending call. RoomId={RoomId}, EndedByConnectionId={ConnectionId}, ParticipantCount={ParticipantCount}",
            roomId,
            sender.ConnectionId,
            participants.Count);

        await _callHistoryService.TryCompleteAcceptedCallAsync(
            roomId,
            sender.UserId,
            sender.ConnectionId,
            endedAtUtc,
            "Call ended explicitly.",
            CancellationToken.None);

        await _hubContext.Clients.Group(roomId).SendAsync("CallEnded", new
        {
            roomId,
            endedByConnectionId = sender.ConnectionId,
            endedByUserId = sender.UserId,
            endedAtUtc
        });

        foreach (var participant in participants)
        {
            await _stateStore.RemoveConnectionFromRoomAsync(participant.ConnectionId);
            await Groups.RemoveFromGroupAsync(participant.ConnectionId, roomId);
        }
    }

    public Task<IceConfigurationDto> GetIceConfiguration()
    {
        var user = RequireAuthenticatedUser();
        var userId = GetUserId(user);
        var iceConfiguration = _iceConfigurationProvider.GetForUser(userId);
        return Task.FromResult(iceConfiguration);
    }

    private async Task JoinConnectionToRoomAsync(string connectionId, string roomId)
    {
        await _stateStore.AddConnectionToRoomAsync(roomId, connectionId);
        await Groups.AddToGroupAsync(connectionId, roomId);
    }

    private async Task<ConnectedUserConnection> GetCurrentConnectionOrThrowAsync()
    {
        var connection = await _stateStore.GetConnectionAsync(Context.ConnectionId);
        if (connection is not null)
            return connection;

        var rebuilt = BuildCurrentConnection();
        await _stateStore.UpsertConnectionAsync(rebuilt);
        return rebuilt;
    }

    private async Task<string> GetCurrentRoomOrThrowAsync(string connectionId)
    {
        var roomId = await _stateStore.GetRoomIdForConnectionAsync(connectionId);
        if (string.IsNullOrWhiteSpace(roomId))
            throw new HubException("You must be in a call room before sending signaling messages.");

        return roomId;
    }

    private async Task EnsureTargetInSameRoomAsync(string targetConnectionId, string roomId)
    {
        var targetConnection = await _stateStore.GetConnectionAsync(targetConnectionId);
        if (targetConnection is null)
            throw new HubException("Target connection is not online.");

        var targetRoomId = await _stateStore.GetRoomIdForConnectionAsync(targetConnectionId);
        if (!string.Equals(targetRoomId, roomId, StringComparison.Ordinal))
            throw new HubException("Target connection is not in the same room.");
    }

    private async Task NotifyJoinedRoomAsync(string roomId)
    {
        var participants = await _stateStore.GetRoomParticipantsAsync(roomId);
        var participantDtos = participants.Select(ToParticipantDto).ToArray();

        foreach (var participant in participants)
        {
            var others = participantDtos
                .Where(x => !string.Equals(x.ConnectionId, participant.ConnectionId, StringComparison.Ordinal))
                .ToArray();

            await _hubContext.Clients.Client(participant.ConnectionId).SendAsync(
                "JoinedRoom",
                new JoinedRoomDto(
                    roomId,
                    participant.ConnectionId,
                    participant.UserId,
                    participant.DisplayName,
                    others));
        }
    }

    private async Task<ParticipantDto[]> GetParticipantDtosAsync(string roomId)
    {
        var participants = await _stateStore.GetRoomParticipantsAsync(roomId);
        return participants.Select(ToParticipantDto).ToArray();
    }

    private static ParticipantDto ToParticipantDto(ConnectedUserConnection connection)
    {
        return new ParticipantDto(connection.ConnectionId, connection.UserId, connection.DisplayName);
    }

    private ConnectedUserConnection BuildCurrentConnection()
    {
        var user = RequireAuthenticatedUser();
        var userId = GetUserId(user);
        var displayName = GetDisplayName(user, userId);

        return new ConnectedUserConnection
        {
            ConnectionId = Context.ConnectionId,
            UserId = userId,
            DisplayName = displayName,
            Roles = GetRoleValues(user),
            IsDispatcher = IsDispatcher(user),
            ConnectedAtUtc = DateTime.UtcNow
        };
    }

    private ClaimsPrincipal RequireAuthenticatedUser()
    {
        var user = Context.User;
        if (user?.Identity?.IsAuthenticated != true)
            throw new HubException("Authentication is required.");

        return user;
    }

    private static string GetUserId(ClaimsPrincipal user)
    {
        var userId = user.FindFirst("sub")?.Value
                     ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            throw new HubException("Authenticated user id claim is missing.");

        return userId;
    }

    private static string GetDisplayName(ClaimsPrincipal user, string fallback)
    {
        var displayName = user.FindFirst("firstName")?.Value
                          ?? user.FindFirst(ClaimTypes.Name)?.Value
                          ?? user.FindFirst(ClaimTypes.Email)?.Value
                          ?? user.FindFirst("email")?.Value;

        return string.IsNullOrWhiteSpace(displayName) ? fallback : displayName.Trim();
    }

    private static bool IsDispatcher(ClaimsPrincipal user)
    {
        return HasRole(GetRoleValues(user), DispatcherRole);
    }

    private static bool HasRole(IEnumerable<string> roles, string role)
    {
        return roles.Any(value =>
            string.Equals(value, role, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasRole(ConnectedUserConnection connection, string role)
    {
        return HasRole(connection.Roles, role);
    }

    private static void ValidateCallTarget(
        ConnectedUserConnection caller,
        IReadOnlyList<ConnectedUserConnection> targetConnections)
    {
        var targetIsDispatcher = targetConnections.Any(connection => connection.IsDispatcher);
        var targetIsStudent = targetConnections.Any(connection => HasRole(connection, StudentRole));

        if (caller.IsDispatcher)
        {
            if (!targetIsDispatcher && targetIsStudent)
                throw new HubException("Dispatchers cannot call students.");

            return;
        }

        if (!targetIsDispatcher)
            throw new HubException("Only dispatchers can call non-dispatcher users.");
    }

    private static string[] GetRoleValues(ClaimsPrincipal? user)
    {
        if (user is null)
            return Array.Empty<string>();

        return user.Claims
            .Where(claim =>
                claim.Type == ClaimTypes.Role ||
                string.Equals(claim.Type, "role", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(claim.Type, "roles", StringComparison.OrdinalIgnoreCase))
            .SelectMany(claim => ExpandRoleValues(claim.Value))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IEnumerable<string> ExpandRoleValues(string rawRoleValue)
    {
        if (string.IsNullOrWhiteSpace(rawRoleValue))
            return Array.Empty<string>();

        var trimmed = rawRoleValue.Trim();

        if (trimmed.StartsWith("[", StringComparison.Ordinal) &&
            trimmed.EndsWith("]", StringComparison.Ordinal))
        {
            try
            {
                using var document = JsonDocument.Parse(trimmed);
                if (document.RootElement.ValueKind == JsonValueKind.Array)
                {
                    return document.RootElement
                        .EnumerateArray()
                        .Where(element => element.ValueKind == JsonValueKind.String)
                        .Select(element => element.GetString())
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Cast<string>()
                        .ToArray();
                }
            }
            catch (JsonException)
            {
                // Fallback to plain string parsing when claim is not valid JSON.
            }
        }

        if (trimmed.Contains(',', StringComparison.Ordinal))
        {
            return trimmed
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();
        }

        return new[] { trimmed };
    }

    private async Task<string> GetTargetDisplayNameAsync(string targetUserId)
    {
        var targetConnections = await _stateStore.GetUserConnectionsAsync(targetUserId);
        return targetConnections.FirstOrDefault()?.DisplayName ?? targetUserId;
    }

    private async Task TryFinalizeAcceptedCallAfterDepartureAsync(
        string roomId,
        string endedByUserId,
        string? endedByConnectionId,
        string reason)
    {
        var remainingParticipants = await _stateStore.GetRoomParticipantsAsync(roomId);
        if (remainingParticipants.Count > 1)
            return;

        var endedAtUtc = DateTime.UtcNow;
        var completedCall = await _callHistoryService.TryCompleteAcceptedCallAsync(
            roomId,
            endedByUserId,
            endedByConnectionId,
            endedAtUtc,
            reason,
            CancellationToken.None);

        if (completedCall is null)
            return;

        if (remainingParticipants.Count > 0)
        {
            await _hubContext.Clients.Group(roomId).SendAsync("CallEnded", new
            {
                roomId,
                endedByConnectionId,
                endedByUserId,
                endedAtUtc
            });
        }

        foreach (var participant in remainingParticipants)
        {
            await _stateStore.RemoveConnectionFromRoomAsync(participant.ConnectionId);
            await Groups.RemoveFromGroupAsync(participant.ConnectionId, roomId);
        }
    }

    private int GetInviteTimeoutSeconds()
    {
        if (_options.InviteTimeoutSeconds < 10)
            return 10;

        if (_options.InviteTimeoutSeconds > 180)
            return 180;

        return _options.InviteTimeoutSeconds;
    }

    private async Task ScheduleTimeoutAsync(string callId, TimeSpan timeout)
    {
        try
        {
            await Task.Delay(timeout);

            var timedOut = await _stateStore.TryTransitionCallAsync(
                callId,
                CallRequestStatus.Pending,
                CallRequestStatus.TimedOut,
                "system",
                null,
                "No response before timeout.",
                DateTime.UtcNow);

            if (timedOut is null)
                return;

            await _callHistoryService.UpdateCallStatusAsync(
                timedOut,
                await GetTargetDisplayNameAsync(timedOut.DispatcherUserId),
                CancellationToken.None);
            await BroadcastTimedOutAsync(timedOut);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process timeout for call {CallId}", callId);
        }
    }

    private async Task BroadcastRejectedAsync(CallRequestState rejected)
    {
        var payload = new CallRejectedDto(
            rejected.CallId,
            rejected.RoomId,
            rejected.DispatcherUserId,
            rejected.ResolvedAtUtc ?? DateTime.UtcNow,
            rejected.ResolveReason);

        await _hubContext.Clients.User(rejected.CallerUserId).SendAsync("CallRejected", payload);
        await _hubContext.Clients.User(rejected.DispatcherUserId).SendAsync("CallRejected", payload);
    }

    private async Task BroadcastCancelledAsync(CallRequestState cancelled)
    {
        var payload = new CallCancelledDto(
            cancelled.CallId,
            cancelled.RoomId,
            cancelled.ResolvedByUserId ?? cancelled.CallerUserId,
            cancelled.ResolvedAtUtc ?? DateTime.UtcNow,
            cancelled.ResolveReason);

        await _hubContext.Clients.User(cancelled.CallerUserId).SendAsync("CallCancelled", payload);
        await _hubContext.Clients.User(cancelled.DispatcherUserId).SendAsync("CallCancelled", payload);
    }

    private async Task BroadcastTimedOutAsync(CallRequestState timedOut)
    {
        var payload = new CallTimedOutDto(
            timedOut.CallId,
            timedOut.RoomId,
            timedOut.ResolvedAtUtc ?? DateTime.UtcNow,
            timedOut.ResolveReason);

        await _hubContext.Clients.User(timedOut.CallerUserId).SendAsync("CallTimedOut", payload);
        await _hubContext.Clients.User(timedOut.DispatcherUserId).SendAsync("CallTimedOut", payload);
    }

    private static string? NormalizeReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return null;

        return reason.Trim();
    }

    private static string ToClientStatus(CallRequestStatus status)
    {
        return status switch
        {
            CallRequestStatus.Pending => "pending",
            CallRequestStatus.Accepted => "accepted",
            CallRequestStatus.Rejected => "rejected",
            CallRequestStatus.Cancelled => "cancelled",
            CallRequestStatus.TimedOut => "timed-out",
            _ => "unknown"
        };
    }

    private string GetDispatcherInboxGroup(string userId)
    {
        var prefix = string.IsNullOrWhiteSpace(_options.DispatcherInboxPrefix)
            ? DefaultDispatcherInboxPrefix
            : _options.DispatcherInboxPrefix.Trim();

        return $"{prefix}{userId.Trim()}";
    }

    private sealed record ParticipantDto(
        string ConnectionId,
        string UserId,
        string DisplayName);

    private sealed record JoinedRoomDto(
        string RoomId,
        string ConnectionId,
        string UserId,
        string DisplayName,
        ParticipantDto[] OtherParticipants);

    private sealed record SignalDescriptionDto(
        string RoomId,
        string FromConnectionId,
        string FromUserId,
        string Sdp);

    private sealed record IceCandidateDto(
        string RoomId,
        string FromConnectionId,
        string FromUserId,
        string Candidate,
        string? SdpMid,
        int? SdpMLineIndex);

    private sealed record IncomingCallDto(
        string CallId,
        string RoomId,
        string FromUserId,
        string FromConnectionId,
        string FromDisplayName,
        DateTime RequestedAtUtc,
        DateTime ExpiresAtUtc);

    private sealed record CallRingingDto(
        string CallId,
        string RoomId,
        string DispatcherUserId,
        string TargetUserId,
        DateTime RequestedAtUtc,
        DateTime ExpiresAtUtc);

    private sealed record CallAcceptedDto(
        string CallId,
        string RoomId,
        string AcceptedByUserId,
        string AcceptedByConnectionId,
        DateTime AcceptedAtUtc,
        ParticipantDto[] Participants);

    private sealed record CallRejectedDto(
        string CallId,
        string RoomId,
        string RejectedByUserId,
        DateTime RejectedAtUtc,
        string? Reason);

    private sealed record CallCancelledDto(
        string CallId,
        string RoomId,
        string CancelledByUserId,
        DateTime CancelledAtUtc,
        string? Reason);

    private sealed record CallTimedOutDto(
        string CallId,
        string RoomId,
        DateTime TimedOutAtUtc,
        string? Reason);
}
