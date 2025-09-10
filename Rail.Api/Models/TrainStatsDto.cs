namespace Rail.Api.Models;

/// <summary>
/// Train statistics DTO
/// </summary>
public class TrainStatsDto
{
    public string NormalizedIndex { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int TotalWagons { get; set; }
    public int LoadedWagons { get; set; }
    public int EmptyWagons { get; set; }
    public decimal TotalWeight { get; set; }
    public decimal AverageWeight { get; set; }
    public decimal MaxWeight { get; set; }
    public decimal MinWeight { get; set; }
    public DateTime EarliestWagonDate { get; set; }
    public DateTime LatestWagonDate { get; set; }
    public List<WagonDto> Wagons { get; set; } = new();
}