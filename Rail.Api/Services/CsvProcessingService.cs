using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Rail.Api.Data;
using Rail.Api.Models;
using Rail.Indexing;

namespace Rail.Api.Services
{
    public class CsvProcessingService : ICsvProcessingService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CsvProcessingService> _logger;
        private readonly ConcurrentDictionary<string, JobStatusDto> _jobs = new();

        public CsvProcessingService(IServiceScopeFactory scopeFactory, ILogger<CsvProcessingService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<string> ProcessCsvAsync(Stream csvStream, CancellationToken cancellationToken = default)
        {
            var jobId = Guid.NewGuid().ToString();
            var jobStatus = new JobStatusDto
            {
                JobId = jobId,
                Status = "Running",
                Progress = 0,
                StartedAt = DateTime.UtcNow
            };

            _jobs[jobId] = jobStatus;
            _logger.LogInformation("Started CSV processing job: {JobId}", jobId);

            // Start background processing
            _ = Task.Run(async () => await ProcessCsvInBackground(jobId, csvStream, cancellationToken), cancellationToken);

            return jobId;
        }

        public JobStatusDto? GetJobStatus(string jobId)
        {
            return _jobs.TryGetValue(jobId, out var status) ? status : null;
        }

        public async Task<CsvUploadResultDto?> GetJobResult(string jobId)
        {
            var status = GetJobStatus(jobId);
            if (status?.Status == "Completed")
            {
                return status.Result;
            }
            return null;
        }

        private async Task ProcessCsvInBackground(string jobId, Stream csvStream, CancellationToken cancellationToken)
        {
            var result = new CsvUploadResultDto
            {
                JobId = jobId,
                Status = "Processing"
            };

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<RailDbContext>();
                var trainService = scope.ServiceProvider.GetRequiredService<ITrainService>();

                var records = await ParseCsvStream(csvStream, cancellationToken);
                result.ProcessedRecords = records.Count;

                _logger.LogInformation("Processing {Count} CSV records for job {JobId}", records.Count, jobId);

                var validRecords = 0;
                var invalidRecords = 0;
                var errors = new List<string>();

                using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

                for (int i = 0; i < records.Count; i++)
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var record = records[i];
                        await ProcessCsvRecord(record, context, trainService, cancellationToken);
                        validRecords++;

                        // Update progress
                        var progress = (int)((i + 1) * 100.0 / records.Count);
                        UpdateJobProgress(jobId, progress);
                    }
                    catch (Exception ex)
                    {
                        invalidRecords++;
                        var errorMsg = $"Row {i + 2}: {ex.Message}";
                        errors.Add(errorMsg);
                        _logger.LogWarning(ex, "Error processing CSV record at row {Row}: {Error}", i + 2, ex.Message);
                    }
                }

                await transaction.CommitAsync(cancellationToken);

                result.Status = "Completed";
                result.ValidRecords = validRecords;
                result.InvalidRecords = invalidRecords;
                result.Errors = errors.Take(50).ToList(); // Limit errors to avoid memory issues
                result.Message = $"Successfully processed {validRecords} records, {invalidRecords} errors";

                UpdateJobCompletion(jobId, result);

                _logger.LogInformation("Completed CSV processing job {JobId}: {Valid} valid, {Invalid} invalid records",
                    jobId, validRecords, invalidRecords);
            }
            catch (OperationCanceledException)
            {
                result.Status = "Cancelled";
                result.Message = "Processing was cancelled";
                UpdateJobCompletion(jobId, result);
                _logger.LogInformation("CSV processing job {JobId} was cancelled", jobId);
            }
            catch (Exception ex)
            {
                result.Status = "Failed";
                result.Message = $"Processing failed: {ex.Message}";
                result.Errors = new List<string> { ex.Message };
                UpdateJobCompletion(jobId, result);
                _logger.LogError(ex, "CSV processing job {JobId} failed", jobId);
            }
        }

        private async Task<List<CsvRecord>> ParseCsvStream(Stream csvStream, CancellationToken cancellationToken)
        {
            var records = new List<CsvRecord>();

            using var reader = new StreamReader(csvStream);
            var headerLine = await reader.ReadLineAsync();

            if (string.IsNullOrWhiteSpace(headerLine))
            {
                throw new InvalidOperationException("CSV file is empty or has no header");
            }

            var headers = ParseCsvLine(headerLine);
            var trainIndexColumn = FindColumnIndex(headers, "TrainIndex", "Index", "TrainCode");
            var wagonNumberColumn = FindColumnIndex(headers, "WagonNumber", "Number", "Wagon");
            var isLoadedColumn = FindColumnIndex(headers, "IsLoaded", "Loaded", "Load");
            var weightColumn = FindColumnIndex(headers, "Weight", "WeightKg", "Kg");
            var dateColumn = FindColumnIndex(headers, "Date", "DateTime", "Time");

            string? line;
            var lineNumber = 2; // Start from 2 since we already read the header

            while ((line = await reader.ReadLineAsync()) != null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    var values = ParseCsvLine(line);

                    var record = new CsvRecord
                    {
                        LineNumber = lineNumber,
                        RawTrainIndex = GetColumnValue(values, trainIndexColumn),
                        WagonNumber = GetColumnValue(values, wagonNumberColumn),
                        IsLoadedText = GetColumnValue(values, isLoadedColumn),
                        WeightText = GetColumnValue(values, weightColumn),
                        DateText = GetColumnValue(values, dateColumn)
                    };

                    records.Add(record);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error parsing line {lineNumber}: {ex.Message}");
                }

                lineNumber++;
            }

            return records;
        }

        private async Task ProcessCsvRecord(CsvRecord record, RailDbContext context, ITrainService trainService, CancellationToken cancellationToken)
        {
            // Normalize train index
            if (string.IsNullOrWhiteSpace(record.RawTrainIndex))
                throw new InvalidOperationException("Train index is required");

            if (!TrainIndex.TryNormalize(record.RawTrainIndex, out var normalizedIndex))
                throw new InvalidOperationException($"Invalid train index: {record.RawTrainIndex}");

            // Parse wagon data
            if (string.IsNullOrWhiteSpace(record.WagonNumber))
                throw new InvalidOperationException("Wagon number is required");

            var isLoaded = ParseBooleanValue(record.IsLoadedText);
            var weight = ParseDecimalValue(record.WeightText);
            var date = ParseDateValue(record.DateText);

            // Get or create train
            var train = await trainService.CreateOrUpdateTrainAsync(normalizedIndex!, cancellationToken);

            // Check if wagon already exists
            var existingWagon = await context.Wagons
                .FirstOrDefaultAsync(w => w.Number == record.WagonNumber && w.TrainId == train.Id, cancellationToken);

            if (existingWagon != null)
            {
                // Update existing wagon
                existingWagon.IsLoaded = isLoaded;
                existingWagon.WeightKg = weight;
                existingWagon.Date = date;
                context.Wagons.Update(existingWagon);
            }
            else
            {
                // Create new wagon
                var wagon = new Wagon
                {
                    Number = record.WagonNumber,
                    IsLoaded = isLoaded,
                    WeightKg = weight,
                    Date = date,
                    TrainId = train.Id
                };

                context.Wagons.Add(wagon);
            }

            await context.SaveChangesAsync(cancellationToken);

            // Invalidate cache for this train
            await trainService.InvalidateTrainStatsCache(normalizedIndex!);
        }

        private string[] ParseCsvLine(string line)
        {
            var values = new List<string>();
            var current = "";
            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(current.Trim());
                    current = "";
                }
                else
                {
                    current += c;
                }
            }

            values.Add(current.Trim());
            return values.ToArray();
        }

        private int FindColumnIndex(string[] headers, params string[] possibleNames)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                var header = headers[i].Trim().Replace("\"", "");
                if (possibleNames.Any(name => string.Equals(header, name, StringComparison.OrdinalIgnoreCase)))
                {
                    return i;
                }
            }
            return -1; // Column not found
        }

        private string GetColumnValue(string[] values, int columnIndex)
        {
            if (columnIndex == -1 || columnIndex >= values.Length)
                return string.Empty;

            return values[columnIndex].Trim().Replace("\"", "");
        }

        private bool ParseBooleanValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            value = value.ToLowerInvariant();
            return value == "true" || value == "1" || value == "yes" || value == "loaded";
        }

        private decimal ParseDecimalValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
                return result;

            throw new InvalidOperationException($"Invalid weight value: {value}");
        }

        private DateTime ParseDateValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return DateTime.UtcNow;

            var formats = new[]
            {
                "yyyy-MM-dd",
                "yyyy-MM-dd HH:mm:ss",
                "dd/MM/yyyy",
                "MM/dd/yyyy",
                "dd-MM-yyyy",
                "MM-dd-yyyy"
            };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                {
                    return DateTime.SpecifyKind(result, DateTimeKind.Utc);
                }
            }

            if (DateTime.TryParse(value, out var fallbackResult))
            {
                return DateTime.SpecifyKind(fallbackResult, DateTimeKind.Utc);
            }

            throw new InvalidOperationException($"Invalid date value: {value}");
        }

        private void UpdateJobProgress(string jobId, int progress)
        {
            if (_jobs.TryGetValue(jobId, out var status))
            {
                status.Progress = progress;
            }
        }

        private void UpdateJobCompletion(string jobId, CsvUploadResultDto result)
        {
            if (_jobs.TryGetValue(jobId, out var status))
            {
                status.Status = result.Status;
                status.CompletedAt = DateTime.UtcNow;
                status.Result = result;
            }
        }
    }

    internal class CsvRecord
    {
        public int LineNumber { get; set; }
        public string RawTrainIndex { get; set; } = string.Empty;
        public string WagonNumber { get; set; } = string.Empty;
        public string IsLoadedText { get; set; } = string.Empty;
        public string WeightText { get; set; } = string.Empty;
        public string DateText { get; set; } = string.Empty;
    }
}