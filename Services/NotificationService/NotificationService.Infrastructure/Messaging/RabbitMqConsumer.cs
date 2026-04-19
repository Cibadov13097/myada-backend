using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Infrastructure.Persistence;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Events.LostFound;
using Shared.Contracts.Events.Notifications;
using Shared.Contracts.Messaging;
using System.Text;
using System.Text.Json;

public class RabbitMqConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RabbitMqConsumer> _logger;
    private readonly ConnectionFactory _factory;

    public RabbitMqConsumer(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<RabbitMqConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var hostName = configuration["RabbitMQ:Host"];
        _factory = new ConnectionFactory
        {
            HostName = string.IsNullOrWhiteSpace(hostName) ? "rabbitmq" : hostName
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var connection = _factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: QueueNames.Notification,
            durable: true,
            exclusive: false,
            autoDelete: false);

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += async (_, ea) =>
        {
            try
            {
                var rawMessage = Encoding.UTF8.GetString(ea.Body.ToArray());
                var notification = BuildNotification(rawMessage);

                if (notification is null)
                {
                    _logger.LogWarning("Unsupported notification payload received from queue.");
                    channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

                dbContext.Notifications.Add(notification);
                await dbContext.SaveChangesAsync(stoppingToken);

                channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                channel.BasicNack(ea.DeliveryTag, false, true);
            }
            catch
            {
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

    private static Notification? BuildNotification(string rawMessage)
    {
        try
        {
            var queueMessage = JsonSerializer.Deserialize<NotificationQueueMessage>(rawMessage);
            if (queueMessage != null && (!string.IsNullOrWhiteSpace(queueMessage.Type) || !string.IsNullOrWhiteSpace(queueMessage.Message)))
            {
                return new Notification
                {
                    Type = string.IsNullOrWhiteSpace(queueMessage.Type) ? "Club Notification" : queueMessage.Type,
                    Message = string.IsNullOrWhiteSpace(queueMessage.Message) ? "Notification received." : queueMessage.Message,
                    NotificationType = NotificationType.Push,
                    CreatedAt = queueMessage.OccurredAtUtc == default ? DateTime.UtcNow : queueMessage.OccurredAtUtc
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
                return new Notification
                {
                    Type = "New Lost Item",
                    Message = $"{lostFoundEvent.Name} at {lostFoundEvent.Location}",
                    NotificationType = NotificationType.Push,
                    CreatedAt = DateTime.UtcNow
                };
            }
        }
        catch (JsonException)
        {
            // Unsupported payload shape.
        }

        return null;
    }
}
