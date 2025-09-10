using Rail.Data.Models;

namespace Rail.RabbitMQ.Services;

public interface IWagonUpdateProcessor
{
    Task<MessageProcessingResult> ProcessWagonUpdateAsync(WagonUpdateMessage message, CancellationToken cancellationToken = default);
    Task<bool> IsEventProcessedAsync(string eventId, CancellationToken cancellationToken = default);
    Task MarkEventAsProcessedAsync(ProcessedEvent processedEvent, CancellationToken cancellationToken = default);
}