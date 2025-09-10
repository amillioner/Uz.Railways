using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Rail.Data.Models;

public class WagonUpdateMessage
{
    [JsonPropertyName("wagon")]
    [Required]
    public string Wagon { get; set; } = string.Empty;

    [JsonPropertyName("load_flag")]
    [Range(0, 1)]
    public int LoadFlag { get; set; }

    [JsonPropertyName("weight")]
    [Range(0, double.MaxValue)]
    public decimal Weight { get; set; }

    [JsonPropertyName("train_index_raw")]
    [Required]
    public string TrainIndexRaw { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    [Required]
    public DateTime Date { get; set; }

    [JsonPropertyName("source")]
    [Required]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("eventId")]
    [Required]
    public string EventId { get; set; } = string.Empty;

    public bool IsLoaded => LoadFlag == 1;
}