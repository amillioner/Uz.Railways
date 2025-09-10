using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Rail.Data.Models;
using System.Text;
using System.Text.Json;

namespace Rail.RabbitMQ.Services;

public class RabbitMqService : IRabbitMqService, IDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqService> _logger;
    private IConnection? _connection;
    private readonly object _connectionLock = new();

    public RabbitMqService(IOptions<RabbitMqSettings> settings, ILogger<RabbitMqService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public IConnection GetConnection()
    {
        if (_connection?.IsOpen == true)
            return _connection;

        lock (_connectionLock)
        {
            if (_connection?.IsOpen == true)
                return _connection;

            var factory = new ConnectionFactory
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection();
            _logger.LogInformation("RabbitMQ connection established to {HostName}:{Port}",
                _settings.HostName, _settings.Port);
        }

        return _connection;
    }

    public async Task PublishAsync<T>(T message, string queueName, CancellationToken cancellationToken = default)
    {
        using var channel = GetConnection().CreateModel();

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.MessageId = Guid.NewGuid().ToString();
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        channel.BasicPublish(
            exchange: _settings.ExchangeName,
            routingKey: queueName,
            basicProperties: properties,
            body: body);

        _logger.LogDebug("Message published to queue {QueueName}: {MessageId}",
            queueName, properties.MessageId);

        await Task.CompletedTask;
    }

    public void SetupQueuesAndExchanges()
    {
        using var channel = GetConnection().CreateModel();

        // Declare exchanges
        channel.ExchangeDeclare(_settings.ExchangeName, ExchangeType.Direct, durable: true);
        channel.ExchangeDeclare(_settings.DeadLetterExchangeName, ExchangeType.Direct, durable: true);

        // Declare main queue with DLQ setup
        var queueArgs = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = _settings.DeadLetterExchangeName,
            ["x-dead-letter-routing-key"] = _settings.DeadLetterQueueName
        };

        channel.QueueDeclare(
            queue: _settings.QueueName,
            durable: _settings.DurableQueues,
            exclusive: false,
            autoDelete: false,
            arguments: queueArgs);

        // Declare dead letter queue
        channel.QueueDeclare(
            queue: _settings.DeadLetterQueueName,
            durable: _settings.DurableQueues,
            exclusive: false,
            autoDelete: false);

        // Bind queues
        channel.QueueBind(_settings.QueueName, _settings.ExchangeName, _settings.QueueName);
        channel.QueueBind(_settings.DeadLetterQueueName, _settings.DeadLetterExchangeName, _settings.DeadLetterQueueName);

        _logger.LogInformation("RabbitMQ queues and exchanges configured successfully");
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}