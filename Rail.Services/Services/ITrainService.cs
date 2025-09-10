using Rail.Data.Models;

namespace Rail.Services.Services;

public interface ITrainService
{
    Task<PagedResponse<TrainDto>> GetTrainsAsync(TrainFilterDto filter, CancellationToken cancellationToken = default);
    Task<TrainStatsDto?> GetTrainStatsAsync(string normalizedIndex, CancellationToken cancellationToken = default);
    Task<Train?> GetTrainByIndexAsync(string normalizedIndex, CancellationToken cancellationToken = default);
    Task<Train> CreateOrUpdateTrainAsync(string normalizedIndex, CancellationToken cancellationToken = default);
    Task InvalidateTrainStatsCache(string normalizedIndex);
}