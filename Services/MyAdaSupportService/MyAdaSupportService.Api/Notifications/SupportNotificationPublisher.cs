using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyAdaSupportService.Application.Abstractions;
using RabbitMQ.Client;
using Shared.Contracts.Events.Notifications;
using Shared.Contracts.Messaging;

namespace MyAdaSupportService.Api.Notifications;

public sealed class SupportNotificationPublisher : ISupportNotificationPublisher
{
    private const string DefaultHost = "rabbitmq";
    private const string DefaultUser = "myada";
    private const string DefaultPass = "myada-password";
    private readonly ConnectionFactory _factory;
    private readonly ILogger<SupportNotificationPublisher> _logger;

    public SupportNotificationPublisher(
        IConfiguration configuration,
        ILogger<SupportNotificationPublisher> logger)
    {
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

    public Task PublishAsync(NotificationQueueMessage message, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        try
        {
            using var connection = _factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(
                queue: QueueNames.Notification,
                durable: true,
                exclusive: false,
                autoDelete: false);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            channel.BasicPublish(
                exchange: string.Empty,
                routingKey: QueueNames.Notification,
                basicProperties: null,
                body: body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish support notification. Continuing without notification.");
        }

        return Task.CompletedTask;
    }
}
