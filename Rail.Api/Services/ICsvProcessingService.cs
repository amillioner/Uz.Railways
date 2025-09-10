using Rail.Api.Models;

namespace Rail.Api.Services;

public interface ICsvProcessingService
{
    Task<string> ProcessCsvAsync(Stream csvStream, CancellationToken cancellationToken = default);
    JobStatusDto? GetJobStatus(string jobId);
    Task<CsvUploadResultDto?> GetJobResult(string jobId);
}