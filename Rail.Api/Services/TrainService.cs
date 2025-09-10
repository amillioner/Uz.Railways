using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Rail.Api.Data;
using Rail.Api.Models;

namespace Rail.Api.Services
{
    public class TrainService : ITrainService
    {
        private readonly RailDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<TrainService> _logger;
        private const int CacheExpirationSeconds = 60;

        public TrainService(RailDbContext context, IMemoryCache cache, ILogger<TrainService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<PagedResponse<TrainDto>> GetTrainsAsync(TrainFilterDto filter, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting trains with filter: {@Filter}", filter);

            var query = _context.Trains.AsQueryable();

            // Apply filters
            query = ApplyFilters(query, filter);

            // Get total count before pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply sorting
            query = ApplySorting(query, filter.SortBy, filter.SortDirection);

            // Apply pagination
            var trains = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(t => new TrainDto
                {
                    Id = t.Id,
                    NormalizedIndex = t.NormalizedIndex,
                    CreatedAt = t.CreatedAt,
                    WagonsCount = t.Wagons.Count,
                    TotalWeight = t.Wagons.Sum(w => w.WeightKg),
                    LoadedWagonsCount = t.Wagons.Count(w => w.IsLoaded)
                })
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} trains out of {Total}", trains.Count, totalCount);

            return new PagedResponse<TrainDto>
            {
                Items = trains,
                TotalCount = totalCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<TrainStatsDto?> GetTrainStatsAsync(string normalizedIndex, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"train_stats_{normalizedIndex}";

            // Try to get from cache first
            if (_cache.TryGetValue(cacheKey, out TrainStatsDto? cachedStats))
            {
                _logger.LogInformation("Retrieved train stats from cache for index: {Index}", normalizedIndex);
                return cachedStats;
            }

            _logger.LogInformation("Getting train stats for index: {Index}", normalizedIndex);

            var train = await _context.Trains
                .Include(t => t.Wagons)
                .FirstOrDefaultAsync(t => t.NormalizedIndex == normalizedIndex, cancellationToken);

            if (train == null)
            {
                _logger.LogWarning("Train with index {Index} not found", normalizedIndex);
                return null;
            }

            var stats = new TrainStatsDto
            {
                NormalizedIndex = train.NormalizedIndex,
                CreatedAt = train.CreatedAt,
                TotalWagons = train.Wagons.Count,
                LoadedWagons = train.Wagons.Count(w => w.IsLoaded),
                EmptyWagons = train.Wagons.Count(w => !w.IsLoaded),
                TotalWeight = train.Wagons.Sum(w => w.WeightKg),
                Wagons = train.Wagons.Select(w => new WagonDto
                {
                    Id = w.Id,
                    Number = w.Number,
                    IsLoaded = w.IsLoaded,
                    WeightKg = w.WeightKg,
                    Date = w.Date,
                    TrainId = w.TrainId,
                    TrainIndex = train.NormalizedIndex
                }).ToList()
            };

            if (train.Wagons.Any())
            {
                var weights = train.Wagons.Select(w => w.WeightKg).ToList();
                stats.AverageWeight = weights.Average();
                stats.MaxWeight = weights.Max();
                stats.MinWeight = weights.Min();
                stats.EarliestWagonDate = train.Wagons.Min(w => w.Date);
                stats.LatestWagonDate = train.Wagons.Max(w => w.Date);
            }

            // Cache the result
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(CacheExpirationSeconds),
                SlidingExpiration = TimeSpan.FromSeconds(CacheExpirationSeconds / 2),
                Priority = CacheItemPriority.Normal
            };

            _cache.Set(cacheKey, stats, cacheOptions);
            _logger.LogInformation("Cached train stats for index: {Index}", normalizedIndex);

            return stats;
        }

        public async Task<Train?> GetTrainByIndexAsync(string normalizedIndex, CancellationToken cancellationToken = default)
        {
            return await _context.Trains
                .FirstOrDefaultAsync(t => t.NormalizedIndex == normalizedIndex, cancellationToken);
        }

        public async Task<Train> CreateOrUpdateTrainAsync(string normalizedIndex, CancellationToken cancellationToken = default)
        {
            var existingTrain = await GetTrainByIndexAsync(normalizedIndex, cancellationToken);

            if (existingTrain != null)
            {
                return existingTrain;
            }

            var newTrain = new Train
            {
                NormalizedIndex = normalizedIndex,
                CreatedAt = DateTime.UtcNow
            };

            _context.Trains.Add(newTrain);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created new train with index: {Index}", normalizedIndex);
            return newTrain;
        }

        public async Task InvalidateTrainStatsCache(string normalizedIndex)
        {
            var cacheKey = $"train_stats_{normalizedIndex}";
            _cache.Remove(cacheKey);
            _logger.LogInformation("Invalidated cache for train index: {Index}", normalizedIndex);
            await Task.CompletedTask;
        }

        private IQueryable<Train> ApplyFilters(IQueryable<Train> query, TrainFilterDto filter)
        {
            if (!string.IsNullOrWhiteSpace(filter.SearchIndex))
            {
                query = query.Where(t => t.NormalizedIndex.Contains(filter.SearchIndex));
            }

            if (filter.CreatedAfter.HasValue)
            {
                query = query.Where(t => t.CreatedAt >= filter.CreatedAfter.Value);
            }

            if (filter.CreatedBefore.HasValue)
            {
                query = query.Where(t => t.CreatedAt <= filter.CreatedBefore.Value);
            }

            if (filter.MinWagons.HasValue)
            {
                query = query.Where(t => t.Wagons.Count >= filter.MinWagons.Value);
            }

            if (filter.MaxWagons.HasValue)
            {
                query = query.Where(t => t.Wagons.Count <= filter.MaxWagons.Value);
            }

            if (filter.MinWeight.HasValue)
            {
                query = query.Where(t => t.Wagons.Sum(w => w.WeightKg) >= filter.MinWeight.Value);
            }

            if (filter.MaxWeight.HasValue)
            {
                query = query.Where(t => t.Wagons.Sum(w => w.WeightKg) <= filter.MaxWeight.Value);
            }

            return query;
        }

        private IQueryable<Train> ApplySorting(IQueryable<Train> query, string sortBy, string sortDirection)
        {
            var isDescending = sortDirection.ToLower() == "desc";

            Expression<Func<Train, object>> keySelector = sortBy.ToLower() switch
            {
                "id" => t => t.Id,
                "normalizedindex" => t => t.NormalizedIndex,
                "createdat" => t => t.CreatedAt,
                "wagonscount" => t => t.Wagons.Count,
                "totalweight" => t => t.Wagons.Sum(w => w.WeightKg),
                _ => t => t.CreatedAt
            };

            return isDescending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);
        }
    }
}