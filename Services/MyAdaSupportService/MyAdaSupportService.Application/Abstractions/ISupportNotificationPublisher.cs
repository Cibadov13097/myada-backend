using Shared.Contracts.Events.Notifications;

namespace MyAdaSupportService.Application.Abstractions;

public interface ISupportNotificationPublisher
{
    Task PublishAsync(NotificationQueueMessage message, CancellationToken cancellationToken = default);
}
