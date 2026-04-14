namespace CallService.Api.Realtime;

public sealed class CallRequestState
{
    public string CallId { get; init; } = string.Empty;
    public string RoomId { get; init; } = string.Empty;
    public string CallerUserId { get; init; } = string.Empty;
    public string CallerConnectionId { get; init; } = string.Empty;
    public string CallerDisplayName { get; init; } = string.Empty;
    public string DispatcherUserId { get; init; } = string.Empty;
    public CallRequestStatus Status { get; set; } = CallRequestStatus.Pending;
    public DateTime RequestedAtUtc { get; init; }
    public DateTime ExpiresAtUtc { get; init; }
    public DateTime? ResolvedAtUtc { get; set; }
    public string? ResolvedByUserId { get; set; }
    public string? ResolvedByConnectionId { get; set; }
    public string? ResolveReason { get; set; }
}
