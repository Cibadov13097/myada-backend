namespace CallService.Api.Realtime;

public sealed class DisconnectedConnectionState
{
    public ConnectedUserConnection Connection { get; init; } = null!;
    public string? RoomId { get; init; }
}
