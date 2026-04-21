namespace Shared.Contracts.Events.Notifications;

public class NotificationQueueMessage
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Channel { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public Guid? ActorUserId { get; set; }
    public Guid? RecipientUserId { get; set; }
    public string? RecipientEmail { get; set; }
    public string? EmailSubject { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
