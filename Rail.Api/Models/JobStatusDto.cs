namespace Rail.Api.Models;

/// <summary>
/// Background job status
/// </summary>
public class JobStatusDto
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Progress { get; set; }
    public string? Message { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public CsvUploadResultDto? Result { get; set; }
}