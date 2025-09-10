using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Rail.Data.Data;
using Rail.Data.Models;
using Rail.Indexing;
using System.ComponentModel.DataAnnotations;

namespace Rail.RabbitMQ.Services;

public class WagonUpdateProcessor : IWagonUpdateProcessor
{
    private readonly RailDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<WagonUpdateProcessor> _logger;

    public WagonUpdateProcessor(
        RailDbContext context,
        IMemoryCache cache,
        ILogger<WagonUpdateProcessor> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }


    public async Task<bool> IsEventProcessedAsync(string eventId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"processed_event_{eventId}";

        if (_cache.TryGetValue(cacheKey, out _))
            return true;

        var exists = await _context.ProcessedEvents
            .AnyAsync(e => e.EventId == eventId, cancellationToken);

        if (exists)
            _cache.Set(cacheKey, true, TimeSpan.FromMinutes(30));

        return exists;
    }

    public async Task<MessageProcessingResult> ProcessWagonUpdateAsync(
        WagonUpdateMessage message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate message
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(message);

            if (!Validator.TryValidateObject(message, validationContext, validationResults, true))
            {
                var errors = string.Join("; ", validationResults.Select(r => r.ErrorMessage));
                return MessageProcessingResult.FailureResult($"Validation failed: {errors}", false);
            }

            // Check for duplicates
            if (await IsEventProcessedAsync(message.EventId, cancellationToken))
            {
                _logger.LogInformation("Event {EventId} already processed, skipping", message.EventId);
                return MessageProcessingResult.DuplicateResult();
            }

            // Normalize train index
            var normalizedIndex = TrainIndex.Normalize(message.TrainIndexRaw);
            if (string.IsNullOrEmpty(normalizedIndex))
            {
                return MessageProcessingResult.FailureResult(
                    $"Invalid train index format: {message.TrainIndexRaw}", false);
            }

            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Find or create train
                var train = await _context.Trains
                    .FirstOrDefaultAsync(t => t.NormalizedIndex == normalizedIndex, cancellationToken);

                if (train == null)
                {
                    train = new Train
                    {
                        NormalizedIndex = normalizedIndex,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Trains.Add(train);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                // Check if wagon already exists for this train
                var existingWagon = await _context.Wagons
                    .FirstOrDefaultAsync(w => w.Number == message.Wagon && w.TrainId == train.Id, cancellationToken);

                Wagon wagon;
                if (existingWagon != null)
                {
                    // Update existing wagon
                    existingWagon.IsLoaded = message.IsLoaded;
                    existingWagon.WeightKg = message.Weight;
                    existingWagon.Date = message.Date;
                    wagon = existingWagon;
                }
                else
                {
                    // Create new wagon
                    wagon = new Wagon
                    {
                        Number = message.Wagon,
                        IsLoaded = message.IsLoaded,
                        WeightKg = message.Weight,
                        Date = message.Date,
                        TrainId = train.Id
                    };
                    _context.Wagons.Add(wagon);
                }

                // Mark event as processed
                var processedEvent = new ProcessedEvent
                {
                    EventId = message.EventId,
                    Source = message.Source,
                    ProcessedAt = DateTime.UtcNow,
                    WagonNumber = message.Wagon,
                    TrainId = train.Id
                };
                _context.ProcessedEvents.Add(processedEvent);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                // Invalidate cache
                InvalidateTrainStatsCache(normalizedIndex);

                _logger.LogInformation(
                    "Successfully processed wagon update: EventId={EventId}, Train={NormalizedIndex}, Wagon={WagonNumber}",
                    message.EventId, normalizedIndex, message.Wagon);

                return MessageProcessingResult.SuccessResult(train.Id, wagon.Id);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing wagon update: {EventId}", message.EventId);
            return MessageProcessingResult.FailureResult(ex.Message, true);
        }
    }

    public async Task MarkEventAsProcessedAsync(ProcessedEvent processedEvent, CancellationToken cancellationToken = default)
    {
        _context.ProcessedEvents.Add(processedEvent);
        await _context.SaveChangesAsync(cancellationToken);

        var cacheKey = $"processed_event_{processedEvent.EventId}";
        _cache.Set(cacheKey, true, TimeSpan.FromMinutes(30));
    }

    private void InvalidateTrainStatsCache(string normalizedIndex)
    {
        var cacheKey = $"train_stats_{normalizedIndex}";
        _cache.Remove(cacheKey);
    }
}