using ClubManagementService.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using Shared.Contracts.Events.Notifications;
using Shared.Contracts.Messaging;
using System.Text;
using System.Text.Json;

namespace ClubManagementService.Infrastructure.Messaging;

public class ClubNotificationService : IClubNotificationService
{
    private readonly ConnectionFactory _factory;

    public ClubNotificationService(IConfiguration configuration)
    {
        var hostName = configuration["RabbitMQ:Host"];
        _factory = new ConnectionFactory
        {
            HostName = string.IsNullOrWhiteSpace(hostName) ? "rabbitmq" : hostName
        };
    }

    public Task PublishAsync(NotificationQueueMessage message)
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

        return Task.CompletedTask;
    }
}
