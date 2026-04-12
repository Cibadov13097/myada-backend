using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace CallService.Api.Hubs;

public class CallHub : Hub
{
    private static readonly ConcurrentDictionary<string, ParticipantConnection> Connections = new();
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ParticipantConnection>> Rooms = new();

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", new { connectionId = Context.ConnectionId });
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await RemoveFromRoomInternal(Context.ConnectionId, notifyOthers: true);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinRoom(string roomId, string userId, string? displayName = null)
    {
        if (string.IsNullOrWhiteSpace(roomId))
            throw new HubException("roomId is required.");

        if (string.IsNullOrWhiteSpace(userId))
            throw new HubException("userId is required.");

        await RemoveFromRoomInternal(Context.ConnectionId, notifyOthers: true);

        var participant = new ParticipantConnection(
            Context.ConnectionId,
            roomId.Trim(),
            userId.Trim(),
            string.IsNullOrWhiteSpace(displayName) ? userId.Trim() : displayName.Trim());

        Connections[Context.ConnectionId] = participant;

        var roomParticipants = Rooms.GetOrAdd(
            participant.RoomId,
            _ => new ConcurrentDictionary<string, ParticipantConnection>());

        roomParticipants[Context.ConnectionId] = participant;
        await Groups.AddToGroupAsync(Context.ConnectionId, participant.RoomId);

        var othersInRoom = roomParticipants.Values
            .Where(x => x.ConnectionId != Context.ConnectionId)
            .Select(x => new ParticipantDto(x.ConnectionId, x.UserId, x.DisplayName))
            .ToArray();

        await Clients.Caller.SendAsync(
            "JoinedRoom",
            new JoinedRoomDto(
                participant.RoomId,
                participant.ConnectionId,
                participant.UserId,
                participant.DisplayName,
                othersInRoom));

        await Clients.OthersInGroup(participant.RoomId).SendAsync(
            "ParticipantJoined",
            new ParticipantDto(participant.ConnectionId, participant.UserId, participant.DisplayName));
    }

    public async Task LeaveRoom()
    {
        var participant = await RemoveFromRoomInternal(Context.ConnectionId, notifyOthers: true);
        if (participant is null)
            return;

        await Clients.Caller.SendAsync("LeftRoom", new
        {
            roomId = participant.RoomId,
            connectionId = participant.ConnectionId
        });
    }

    public async Task SendOffer(string targetConnectionId, string sdp)
    {
        var sender = GetCurrentParticipant();
        var target = GetTargetInSameRoom(targetConnectionId, sender.RoomId);

        await Clients.Client(target.ConnectionId).SendAsync(
            "ReceiveOffer",
            new SignalDescriptionDto(sender.RoomId, sender.ConnectionId, sender.UserId, sdp));
    }

    public async Task SendAnswer(string targetConnectionId, string sdp)
    {
        var sender = GetCurrentParticipant();
        var target = GetTargetInSameRoom(targetConnectionId, sender.RoomId);

        await Clients.Client(target.ConnectionId).SendAsync(
            "ReceiveAnswer",
            new SignalDescriptionDto(sender.RoomId, sender.ConnectionId, sender.UserId, sdp));
    }

    public async Task SendIceCandidate(
        string targetConnectionId,
        string candidate,
        string? sdpMid,
        int? sdpMLineIndex)
    {
        var sender = GetCurrentParticipant();
        var target = GetTargetInSameRoom(targetConnectionId, sender.RoomId);

        await Clients.Client(target.ConnectionId).SendAsync(
            "ReceiveIceCandidate",
            new IceCandidateDto(
                sender.RoomId,
                sender.ConnectionId,
                sender.UserId,
                candidate,
                sdpMid,
                sdpMLineIndex));
    }

    public async Task EndCall()
    {
        var sender = GetCurrentParticipant();

        await Clients.Group(sender.RoomId).SendAsync("CallEnded", new
        {
            roomId = sender.RoomId,
            endedByConnectionId = sender.ConnectionId,
            endedByUserId = sender.UserId
        });
    }

    private ParticipantConnection GetCurrentParticipant()
    {
        if (!Connections.TryGetValue(Context.ConnectionId, out var participant))
            throw new HubException("You must join a room before sending signaling messages.");

        return participant;
    }

    private static ParticipantConnection GetTargetInSameRoom(string targetConnectionId, string roomId)
    {
        if (string.IsNullOrWhiteSpace(targetConnectionId))
            throw new HubException("targetConnectionId is required.");

        if (!Connections.TryGetValue(targetConnectionId, out var target))
            throw new HubException("Target connection is not online.");

        if (!string.Equals(target.RoomId, roomId, StringComparison.Ordinal))
            throw new HubException("Target connection is not in the same room.");

        return target;
    }

    private async Task<ParticipantConnection?> RemoveFromRoomInternal(string connectionId, bool notifyOthers)
    {
        if (!Connections.TryRemove(connectionId, out var participant))
            return null;

        await Groups.RemoveFromGroupAsync(connectionId, participant.RoomId);

        if (Rooms.TryGetValue(participant.RoomId, out var roomParticipants))
        {
            roomParticipants.TryRemove(connectionId, out _);

            if (roomParticipants.IsEmpty)
                Rooms.TryRemove(participant.RoomId, out _);
        }

        if (notifyOthers)
        {
            await Clients.OthersInGroup(participant.RoomId).SendAsync(
                "ParticipantLeft",
                new ParticipantDto(participant.ConnectionId, participant.UserId, participant.DisplayName));
        }

        return participant;
    }

    private sealed record ParticipantConnection(
        string ConnectionId,
        string RoomId,
        string UserId,
        string DisplayName);

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
}
