using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Infrastructure.Persistence;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Events.LostFound;
using Shared.Contracts.Events.Notifications;
using Shared.Contracts.Messaging;

namespace NotificationService.Infrastructure.Messaging;

public class RabbitMqConsumer : BackgroundService
{
    private const string DefaultHost = "rabbitmq";
    private const string DefaultUser = "myada";
    private const string DefaultPass = "myada-password";
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RabbitMqConsumer> _logger;
    private readonly ConnectionFactory _factory;

    public RabbitMqConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<RabbitMqConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var hostName = configuration["RabbitMQ:Host"];
        var userName = configuration["RabbitMQ:Username"];
        var password = configuration["RabbitMQ:Password"];
        _factory = new ConnectionFactory
        {
            HostName = string.IsNullOrWhiteSpace(hostName) ? DefaultHost : hostName,
            UserName = string.IsNullOrWhiteSpace(userName) ? DefaultUser : userName,
            Password = string.IsNullOrWhiteSpace(password) ? DefaultPass : password
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var connection = _factory.CreateConnection();
                using var channel = connection.CreateModel();

                channel.QueueDeclare(
                    queue: QueueNames.Notification,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);

                _logger.LogInformation(
                    "RabbitMQ notification consumer connected to host '{HostName}' and is listening on queue '{QueueName}'.",
                    _factory.HostName,
                    QueueNames.Notification);

                var consumer = new EventingBasicConsumer(channel);

                consumer.Received += async (_, ea) =>
                {
                    try
                    {
                        var rawMessage = Encoding.UTF8.GetString(ea.Body.ToArray());
                        var envelope = BuildNotification(rawMessage);

                        if (envelope is null)
                        {
                            _logger.LogWarning("Unsupported notification payload received from queue.");
                            channel.BasicAck(ea.DeliveryTag, false);
                            return;
                        }

                        await PersistAndDispatchAsync(envelope, stoppingToken);
                        channel.BasicAck(ea.DeliveryTag, false);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        channel.BasicNack(ea.DeliveryTag, false, true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process notification queue message.");
                        channel.BasicNack(ea.DeliveryTag, false, true);
                    }
                };

                channel.BasicConsume(
                    queue: QueueNames.Notification,
                    autoAck: false,
                    consumer: consumer);

                try
                {
                    await Task.Delay(Timeout.Infinite, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("RabbitMQ notification consumer is stopping.");
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    ex,
                    "RabbitMQ notification consumer could not connect to host '{HostName}'. Retrying in {RetryDelaySeconds} seconds.",
                    _factory.HostName,
                    RetryDelay.TotalSeconds);

                try
                {
                    await Task.Delay(RetryDelay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    private async Task PersistAndDispatchAsync(NotificationEnvelope envelope, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        dbContext.Notifications.Add(envelope.Notification);
        await dbContext.SaveChangesAsync(cancellationToken);

        var realtimeNotifier = scope.ServiceProvider.GetRequiredService<INotificationRealtimeNotifier>();
        try
        {
            await realtimeNotifier.NotifyNotificationCreatedAsync(envelope.Notification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to broadcast realtime notification {NotificationId}. Continuing with notification processing.",
                envelope.Notification.Id);
        }

        if (envelope.Notification.NotificationType != NotificationType.Email)
            return;

        if (string.IsNullOrWhiteSpace(envelope.RecipientEmail))
        {
            _logger.LogWarning(
                "Email notification was requested but no recipient email was provided. Type: {Type}",
                envelope.Notification.Type);
            return;
        }

        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        try
        {
            await notificationService.SendEmailAsync(
                envelope.RecipientEmail,
                envelope.Subject,
                envelope.Notification.Message,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send email notification to {Recipient}. Notification was still persisted.",
                envelope.RecipientEmail);
        }
    }

    private static NotificationEnvelope? BuildNotification(string rawMessage)
    {
        try
        {
            var queueMessage = JsonSerializer.Deserialize<NotificationQueueMessage>(rawMessage);
            if (queueMessage != null && (!string.IsNullOrWhiteSpace(queueMessage.Type) || !string.IsNullOrWhiteSpace(queueMessage.Message)))
            {
                var createdAt = queueMessage.OccurredAtUtc == default ? DateTime.UtcNow : queueMessage.OccurredAtUtc;
                var notificationType = ResolveNotificationType(queueMessage.Channel, queueMessage.RecipientEmail);
                var fallbackType = string.IsNullOrWhiteSpace(queueMessage.Type) ? "Club Notification" : queueMessage.Type;

                return new NotificationEnvelope
                {
                    Notification = new Notification
                    {
                        Type = fallbackType,
                        Message = string.IsNullOrWhiteSpace(queueMessage.Message) ? "Notification received." : queueMessage.Message,
                        NotificationType = notificationType,
                        CreatedAt = createdAt,
                        RecipientUserId = queueMessage.RecipientUserId
                    },
                    RecipientEmail = queueMessage.RecipientEmail,
                    Subject = string.IsNullOrWhiteSpace(queueMessage.EmailSubject)
                        ? fallbackType
                        : queueMessage.EmailSubject
                };
            }
        }
        catch (JsonException)
        {
            // Fall back to legacy contract support below.
        }

        try
        {
            var lostFoundEvent = JsonSerializer.Deserialize<LostItemCreatedEvent>(rawMessage);
            if (lostFoundEvent != null && !string.IsNullOrWhiteSpace(lostFoundEvent.Name))
            {
                return new NotificationEnvelope
                {
                    Notification = new Notification
                    {
                        Type = "New Lost Item",
                        Message = $"{lostFoundEvent.Name} at {lostFoundEvent.Location}",
                        NotificationType = NotificationType.Push,
                        CreatedAt = DateTime.UtcNow,
                        RecipientUserId = null
                    },
                    Subject = "New Lost Item"
                };
            }
        }
        catch (JsonException)
        {
            // Unsupported payload shape.
        }

        return null;
    }

    private static NotificationType ResolveNotificationType(string? channel, string? recipientEmail)
    {
        if (!string.IsNullOrWhiteSpace(channel) &&
            Enum.TryParse<NotificationType>(channel, true, out var parsedChannel))
        {
            return parsedChannel;
        }

        return string.IsNullOrWhiteSpace(recipientEmail)
            ? NotificationType.Push
            : NotificationType.Email;
    }

    private sealed class NotificationEnvelope
    {
        public required Notification Notification { get; init; }
        public string? RecipientEmail { get; init; }
        public string Subject { get; init; } = "MyADA Notification";
    }
}
