using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Shared.Shared.Infrastructure;


public class RabbitMqPublisher : IRabbitMqPublisher
{
    private const string DefaultHost = "rabbitmq";
    private const string DefaultUser = "myada";
    private const string DefaultPass = "myada-password";
    private readonly ConnectionFactory _factory;

    public RabbitMqPublisher(IConfiguration configuration)
    {
        var hostName = configuration["RabbitMQ:Host"];
        var userName = configuration["RabbitMQ:Username"];
        var password = configuration["RabbitMQ:Password"];

        _factory = new ConnectionFactory()
        {
            HostName = string.IsNullOrWhiteSpace(hostName) ? DefaultHost : hostName,
            UserName = string.IsNullOrWhiteSpace(userName) ? DefaultUser : userName,
            Password = string.IsNullOrWhiteSpace(password) ? DefaultPass : password
        };
    }

    public async Task PublishAsync<T>(string queueName, T message)
    {
        using var connection = _factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        channel.BasicPublish(
            exchange: "",
            routingKey: queueName,
            basicProperties: null,
            body: body);
    }
}
