using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Rail.Data.Models;
using System.Text;
using System.Text.Json;

namespace Rail.RabbitMQ.Services;

public class WagonUpdateConsumer : BackgroundService
{
    private readonly IRabbitMqService _rabbitMqService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<WagonUpdateConsumer> _logger;
    private IModel? _channel;

    public WagonUpdateConsumer(
        IRabbitMqService rabbitMqService,
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqSettings> settings,
        ILogger<WagonUpdateConsumer> logger)
    {
        _rabbitMqService = rabbitMqService;
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        _logger.LogInformation("Starting wagon update consumer");

        try
        {
            var connection = _rabbitMqService.GetConnection();
            _channel = connection.CreateModel();
            _channel.BasicQos(0, (ushort)_settings.PrefetchCount, false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += OnMessageReceived;

            _channel.BasicConsume(
                queue: _settings.QueueName,
                autoAck: _settings.AutoAck,
                consumer: consumer);

            _logger.LogInformation("Wagon update consumer started, waiting for messages");

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Wagon update consumer is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in wagon update consumer");
            throw;
        }
    }

    private async Task OnMessageReceived(object sender, BasicDeliverEventArgs ea)
    {
        var deliveryTag = ea.DeliveryTag;
        var redelivered = ea.Redelivered;

        try
        {
            var body = ea.Body.ToArray();
            var messageText = Encoding.UTF8.GetString(body);

            _logger.LogDebug("Received message: {Message}", messageText);

            var message = JsonSerializer.Deserialize<WagonUpdateMessage>(messageText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (message == null)
            {
                _logger.LogError("Failed to deserialize message: {Message}", messageText);
                _channel?.BasicNack(deliveryTag, false, false); // Send to DLQ
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IWagonUpdateProcessor>();

            var result = await processor.ProcessWagonUpdateAsync(message);

            if (result.Success)
            {
                _channel?.BasicAck(deliveryTag, false);
                _logger.LogDebug("Message processed successfully: {EventId}", message.EventId);
            }
            else
            {
                if (result.ShouldRetry && !redelivered)
                {
                    _logger.LogWarning("Message processing failed, will retry: {EventId}, Error: {Error}",
                        message.EventId, result.ErrorMessage);
                    _channel?.BasicNack(deliveryTag, false, true); // Requeue
                }
                else
                {
                    _logger.LogError("Message processing failed permanently: {EventId}, Error: {Error}",
                        message.EventId, result.ErrorMessage);
                    _channel?.BasicNack(deliveryTag, false, false); // Send to DLQ
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing message");
            _channel?.BasicNack(deliveryTag, false, !redelivered); // Retry once, then DLQ
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        base.Dispose();
    }
}