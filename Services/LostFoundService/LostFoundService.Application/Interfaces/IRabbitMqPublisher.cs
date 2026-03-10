namespace  Shared.Shared.Infrastructure;

public interface IRabbitMqPublisher
{
    Task PublishAsync<T>(string queueName,T message );
}
