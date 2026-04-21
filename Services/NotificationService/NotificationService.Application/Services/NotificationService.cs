using NotificationService.Application.Interfaces;

namespace NotificationService.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IEmailSender _emailSender;

    public NotificationService(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public Task SendEmailAsync(
        string email,
        string subject,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Recipient email is required.", nameof(email));

        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Email subject is required.", nameof(subject));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Email message is required.", nameof(message));

        return _emailSender.SendAsync(email, subject, message, cancellationToken);
    }
}
