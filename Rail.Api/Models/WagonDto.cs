namespace Rail.Api.Models;

/// <summary>
/// Wagon DTO for API responses
/// </summary>
public class WagonDto
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public bool IsLoaded { get; set; }
    public decimal WeightKg { get; set; }
    public DateTime Date { get; set; }
    public int TrainId { get; set; }
    public string TrainIndex { get; set; } = string.Empty;
}