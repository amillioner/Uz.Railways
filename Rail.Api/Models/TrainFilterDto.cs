namespace Rail.Api.Models;

/// <summary>
/// Train filter parameters
/// </summary>
public class TrainFilterDto
{
    public string? SearchIndex { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public int? MinWagons { get; set; }
    public int? MaxWagons { get; set; }
    public decimal? MinWeight { get; set; }
    public decimal? MaxWeight { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public string SortDirection { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}