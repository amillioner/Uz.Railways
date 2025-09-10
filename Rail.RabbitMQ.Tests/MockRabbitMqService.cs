namespace RailApi.Tests.IntegrationTests.RailApi.Tests.IntegrationTests;

public class MockRabbitMqService : IRabbitMqService
{
    private readonly List<object> _publishedMessages = new();
    public IReadOnlyList<object> PublishedMessages => _publishedMessages.AsReadOnly();

    public IConnection GetConnection()
    {
        // Return a mock connection or null for testing
        return null!;
    }

    public Task PublishAsync<T>(T message, string queueName, CancellationToken cancellationToken = default)
    {
        _publishedMessages.Add(message!);
        return Task.CompletedTask;
    }

    public void SetupQueuesAndExchanges()
    {
        // No-op for testing
    }

    public void ClearMessages()
    {
        _publishedMessages.Clear();
    }
}