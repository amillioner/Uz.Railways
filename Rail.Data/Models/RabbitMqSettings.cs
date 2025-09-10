namespace Rail.Data.Models;

public class RabbitMqSettings
{
    public const string SectionName = "RabbitMQ";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string QueueName { get; set; } = "rail.wagon.updates";
    public string DeadLetterQueueName { get; set; } = "rail.wagon.updates.dlq";
    public string ExchangeName { get; set; } = "rail.exchange";
    public string DeadLetterExchangeName { get; set; } = "rail.exchange.dlq";
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 5;
    public int PrefetchCount { get; set; } = 10;
    public bool DurableQueues { get; set; } = true;
    public bool AutoAck { get; set; } = false;
}