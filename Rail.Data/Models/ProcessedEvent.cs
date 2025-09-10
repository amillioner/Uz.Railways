using System;
using System.ComponentModel.DataAnnotations;

namespace Rail.Data.Models;

public class ProcessedEvent
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string EventId { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Source { get; set; } = string.Empty;

    public DateTime ProcessedAt { get; set; }

    [MaxLength(50)]
    public string WagonNumber { get; set; } = string.Empty;

    public int? TrainId { get; set; }
    public Train? Train { get; set; }
}