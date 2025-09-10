using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rail.Data.Models;

/// <summary>
/// Wagon entity
/// </summary>
public class Wagon
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string Number { get; set; } = string.Empty;

    public bool IsLoaded { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal WeightKg { get; set; }

    public DateTime Date { get; set; }

    // Foreign key
    public int TrainId { get; set; }

    // Navigation property
    public Train Train { get; set; } = null!;
}