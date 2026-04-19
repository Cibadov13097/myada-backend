using Shared.Contracts.Events.Notifications;

namespace ClubManagementService.Application.Interfaces.Services;

public interface IClubNotificationService
{
    Task PublishAsync(NotificationQueueMessage message);
}
