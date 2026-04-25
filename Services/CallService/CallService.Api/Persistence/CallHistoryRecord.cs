using CallService.Api.Realtime;

namespace CallService.Api.Persistence;

public sealed class CallHistoryRecord
{
    public Guid Id { get; set; }
    public string CallId { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public string CallerUserId { get; set; } = string.Empty;
    public string CallerDisplayName { get; set; } = string.Empty;
    public string DispatcherUserId { get; set; } = string.Empty;
    public string DispatcherDisplayName { get; set; } = string.Empty;
    public string CallerConnectionId { get; set; } = string.Empty;
    public string? DispatcherConnectionId { get; set; }
    public CallRequestStatus Status { get; set; } = CallRequestStatus.Pending;
    public DateTime RequestedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? AcceptedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public long? DurationSeconds { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public string? ResolvedByUserId { get; set; }
    public string? ResolvedByConnectionId { get; set; }
    public string? ResolveReason { get; set; }
    public string? EndedByUserId { get; set; }
    public string? EndedByConnectionId { get; set; }
    public string? EndReason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
