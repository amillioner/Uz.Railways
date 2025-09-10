namespace Rail.Api.Models;

/// <summary>
/// CSV upload result DTO
/// </summary>
public class CsvUploadResultDto
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int ProcessedRecords { get; set; }
    public int ValidRecords { get; set; }
    public int InvalidRecords { get; set; }
    public List<string> Errors { get; set; } = new();
}