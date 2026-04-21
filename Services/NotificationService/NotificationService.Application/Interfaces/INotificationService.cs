namespace NotificationService.Application.Interfaces;

public interface INotificationService
{
    Task SendEmailAsync(
        string email,
        string subject,
        string message,
        CancellationToken cancellationToken = default);
}
