namespace CallService.Api.Realtime;

public sealed class ConnectedUserConnection
{
    public string ConnectionId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public bool IsDispatcher { get; init; }
    public DateTime ConnectedAtUtc { get; init; }
}
