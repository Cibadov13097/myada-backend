using System.Security.Claims;
using CallService.Api.Realtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace CallService.Api.Hubs;

[Authorize]
public class CallHub : Hub
{
    private const string DispatcherRole = "dispatcher";

    private readonly ICallStateStore _stateStore;
    private readonly IHubContext<CallHub> _hubContext;
    private readonly ILogger<CallHub> _logger;
    private readonly CallRuntimeOptions _options;

    public CallHub(
        ICallStateStore stateStore,
        IHubContext<CallHub> hubContext,
        IOptions<CallRuntimeOptions> options,
        ILogger<CallHub> logger)
    {
        _stateStore = stateStore;
        _hubContext = hubContext;
        _logger = logger;
        _options = options.Value;
    }

    public override async Task OnConnectedAsync()
    {
        var connection = BuildCurrentConnection();
        await _stateStore.UpsertConnectionAsync(connection);

        var roles = Context.User?.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? Array.Empty<string>();

        await Clients.Caller.SendAsync("Connected", new
        {
            connectionId = connection.ConnectionId,
            userId = connection.UserId,
            displayName = connection.DisplayName,
            isDispatcher = connection.IsDispatcher,
            roles
        });

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var disconnected = await _stateStore.RemoveConnectionAsync(Context.ConnectionId);

        if (!string.IsNullOrWhiteSpace(disconnected?.RoomId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, disconnected.RoomId);
            await Clients.OthersInGroup(disconnected.RoomId).SendAsync(
                "ParticipantLeft",
                new ParticipantDto(
                    disconnected.Connection.ConnectionId,
                    disconnected.Connection.UserId,
                    disconnected.Connection.DisplayName));
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task RequestCall(string dispatcherUserId)
    {
        if (string.IsNullOrWhiteSpace(dispatcherUserId))
            throw new HubException("dispatcherUserId is required.");

        var caller = await GetCurrentConnectionOrThrowAsync();
        dispatcherUserId = dispatcherUserId.Trim();

        if (string.Equals(caller.UserId, dispatcherUserId, StringComparison.Ordinal))
            throw new HubException("You cannot call yourself.");

        var dispatcherConnections = await _stateStore.GetUserConnectionsAsync(dispatcherUserId);
        if (dispatcherConnections.Count == 0)
            throw new HubException("Dispatcher is offline.");

        if (!dispatcherConnections.Any(x => x.IsDispatcher))
            throw new HubException("Target user does not have dispatcher role.");

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
            DispatcherUserId = dispatcherUserId,
            Status = CallRequestStatus.Pending,
            RequestedAtUtc = nowUtc,
            ExpiresAtUtc = expiresAtUtc
        };

        await _stateStore.CreatePendingCallAsync(pendingCall);

        var incomingPayload = new IncomingCallDto(
            pendingCall.CallId,
            pendingCall.RoomId,
            pendingCall.CallerUserId,
            pendingCall.CallerConnectionId,
            pendingCall.CallerDisplayName,
            pendingCall.RequestedAtUtc,
            pendingCall.ExpiresAtUtc);

        await _hubContext.Clients.User(dispatcherUserId).SendAsync("IncomingCall", incomingPayload);

        await _hubContext.Clients.User(caller.UserId).SendAsync(
            "CallRinging",
            new CallRingingDto(
                pendingCall.CallId,
                pendingCall.RoomId,
                pendingCall.DispatcherUserId,
                pendingCall.RequestedAtUtc,
                pendingCall.ExpiresAtUtc));

        _ = ScheduleTimeoutAsync(pendingCall.CallId, timeout);
    }

    [Authorize(Roles = DispatcherRole)]
    public async Task AcceptCall(string callId)
    {
        if (string.IsNullOrWhiteSpace(callId))
            throw new HubException("callId is required.");

        var dispatcher = await GetCurrentConnectionOrThrowAsync();
        if (!dispatcher.IsDispatcher)
            throw new HubException("Only dispatchers can accept calls.");

        var existingCall = await _stateStore.GetCallAsync(callId);
        if (existingCall is null)
            throw new HubException("Call request not found.");

        if (!string.Equals(existingCall.DispatcherUserId, dispatcher.UserId, StringComparison.Ordinal))
            throw new HubException("This call was not requested for the current dispatcher.");

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
                await BroadcastTimedOutAsync(timedOut);

            throw new HubException("Call request expired.");
        }

        var accepted = await _stateStore.TryTransitionCallAsync(
            existingCall.CallId,
            CallRequestStatus.Pending,
            CallRequestStatus.Accepted,
            dispatcher.UserId,
            dispatcher.ConnectionId,
            null,
            DateTime.UtcNow);

        if (accepted is null)
            throw new HubException("Call request state changed. Please refresh and try again.");

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
                await BroadcastCancelledAsync(cancelled);

            throw new HubException("Caller is no longer online.");
        }

        await JoinConnectionToRoomAsync(callerConnection.ConnectionId, accepted.RoomId);
        await JoinConnectionToRoomAsync(dispatcher.ConnectionId, accepted.RoomId);

        await NotifyJoinedRoomAsync(accepted.RoomId);

        var participants = await GetParticipantDtosAsync(accepted.RoomId);
        var acceptedAtUtc = accepted.ResolvedAtUtc ?? DateTime.UtcNow;

        var payload = new CallAcceptedDto(
            accepted.CallId,
            accepted.RoomId,
            dispatcher.UserId,
            dispatcher.ConnectionId,
            acceptedAtUtc,
            participants);

        await _hubContext.Clients.User(accepted.CallerUserId).SendAsync("CallAccepted", payload);
        await _hubContext.Clients.User(accepted.DispatcherUserId).SendAsync("CallAccepted", payload);
    }

    [Authorize(Roles = DispatcherRole)]
    public async Task RejectCall(string callId, string? reason = null)
    {
        if (string.IsNullOrWhiteSpace(callId))
            throw new HubException("callId is required.");

        var dispatcher = await GetCurrentConnectionOrThrowAsync();
        if (!dispatcher.IsDispatcher)
            throw new HubException("Only dispatchers can reject calls.");

        var existingCall = await _stateStore.GetCallAsync(callId);
        if (existingCall is null)
            throw new HubException("Call request not found.");

        if (!string.Equals(existingCall.DispatcherUserId, dispatcher.UserId, StringComparison.Ordinal))
            throw new HubException("This call was not requested for the current dispatcher.");

        var rejected = await _stateStore.TryTransitionCallAsync(
            existingCall.CallId,
            CallRequestStatus.Pending,
            CallRequestStatus.Rejected,
            dispatcher.UserId,
            dispatcher.ConnectionId,
            NormalizeReason(reason),
            DateTime.UtcNow);

        if (rejected is null)
            throw new HubException("Call request state changed. Please refresh and try again.");

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

        await _hubContext.Clients.Group(roomId).SendAsync("CallEnded", new
        {
            roomId,
            endedByConnectionId = sender.ConnectionId,
            endedByUserId = sender.UserId,
            endedAtUtc = DateTime.UtcNow
        });

        foreach (var participant in participants)
        {
            await _stateStore.RemoveConnectionFromRoomAsync(participant.ConnectionId);
            await Groups.RemoveFromGroupAsync(participant.ConnectionId, roomId);
        }
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
        return user.Claims.Any(c =>
            c.Type == ClaimTypes.Role &&
            string.Equals(c.Value, DispatcherRole, StringComparison.OrdinalIgnoreCase));
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
                "No dispatcher response before timeout.",
                DateTime.UtcNow);

            if (timedOut is null)
                return;

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
