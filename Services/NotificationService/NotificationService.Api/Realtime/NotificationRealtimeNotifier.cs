using Microsoft.AspNetCore.SignalR;
using NotificationService.Api.Hubs;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;

namespace NotificationService.Api.Realtime;

public sealed class NotificationRealtimeNotifier : INotificationRealtimeNotifier
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationRealtimeNotifier(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyNotificationCreatedAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        if (notification.RecipientUserId is not Guid recipientUserId || recipientUserId == Guid.Empty)
            return Task.CompletedTask;

        return _hubContext.Clients.User(recipientUserId.ToString()).SendAsync(
            "notificationCreated",
            new
            {
                id = notification.Id,
                type = notification.Type,
                message = notification.Message,
                channel = notification.NotificationType.ToString(),
                recipientUserId = notification.RecipientUserId,
                createdAt = notification.CreatedAt
            },
            cancellationToken);
    }
}
