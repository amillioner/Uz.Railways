namespace Rail.Api.Models;

/// <summary>
/// API error response
/// </summary>
public class ApiErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<string> Details { get; set; } = new();
    public string TraceId { get; set; } = string.Empty;
}