namespace Rail.Api.Models;

/// <summary>
/// Train DTO for API responses
/// </summary>
public class TrainDto
{
    public int Id { get; set; }
    public string NormalizedIndex { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int WagonsCount { get; set; }
    public decimal TotalWeight { get; set; }
    public int LoadedWagonsCount { get; set; }
}