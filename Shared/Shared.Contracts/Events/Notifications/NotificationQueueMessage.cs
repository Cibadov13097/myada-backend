namespace Shared.Contracts.Events.Notifications;

public class NotificationQueueMessage
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public Guid? ActorUserId { get; set; }
    public Guid? RecipientUserId { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
