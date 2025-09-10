using RabbitMQ.Client;

namespace Rail.RabbitMQ.Services;

public interface IRabbitMqService
{
    IConnection GetConnection();
    Task PublishAsync<T>(T message, string queueName, CancellationToken cancellationToken = default);
    void SetupQueuesAndExchanges();
}