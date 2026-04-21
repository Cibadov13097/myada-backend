using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Interfaces;

namespace NotificationService.Infrastructure.Email;

public class EmailSender : IEmailSender
{
    private readonly SmtpOptions _smtpOptions;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IOptions<SmtpOptions> smtpOptions, ILogger<EmailSender> logger)
    {
        _smtpOptions = smtpOptions.Value;
        _logger = logger;
    }

    public async Task SendAsync(
        string email,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        var fromEmail = ResolveFromEmail();
        ValidateConfiguration(fromEmail);

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, _smtpOptions.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(email));

        using var smtpClient = new SmtpClient(_smtpOptions.Host, _smtpOptions.Port)
        {
            DeliveryMethod = SmtpDeliveryMethod.Network,
            EnableSsl = _smtpOptions.EnableSsl,
            UseDefaultCredentials = _smtpOptions.UseDefaultCredentials
        };

        if (!_smtpOptions.UseDefaultCredentials && !string.IsNullOrWhiteSpace(_smtpOptions.Username))
        {
            smtpClient.Credentials = new NetworkCredential(_smtpOptions.Username, _smtpOptions.Password);
        }

        cancellationToken.ThrowIfCancellationRequested();
        await smtpClient.SendMailAsync(message);
        _logger.LogInformation("Email notification sent to {Recipient}.", email);
    }

    private string ResolveFromEmail()
    {
        return string.IsNullOrWhiteSpace(_smtpOptions.FromEmail)
            ? _smtpOptions.Username
            : _smtpOptions.FromEmail;
    }

    private void ValidateConfiguration(string fromEmail)
    {
        if (string.IsNullOrWhiteSpace(_smtpOptions.Host))
            throw new InvalidOperationException("SMTP configuration is missing Smtp:Host.");

        if (_smtpOptions.Port <= 0)
            throw new InvalidOperationException("SMTP configuration has invalid Smtp:Port.");

        if (!_smtpOptions.UseDefaultCredentials && string.IsNullOrWhiteSpace(_smtpOptions.Username))
            throw new InvalidOperationException("SMTP configuration is missing Smtp:Username.");

        if (!_smtpOptions.UseDefaultCredentials && string.IsNullOrWhiteSpace(_smtpOptions.Password))
            throw new InvalidOperationException("SMTP configuration is missing Smtp:Password.");

        if (string.IsNullOrWhiteSpace(fromEmail))
            throw new InvalidOperationException("SMTP configuration is missing Smtp:FromEmail.");
    }
}
