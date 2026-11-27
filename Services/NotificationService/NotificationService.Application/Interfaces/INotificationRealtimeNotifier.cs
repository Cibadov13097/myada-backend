using NotificationService.Domain.Entities;

namespace NotificationService.Application.Interfaces;

public interface INotificationRealtimeNotifier
{
    Task NotifyNotificationCreatedAsync(
        Notification notification,
        CancellationToken cancellationToken = default);
}
