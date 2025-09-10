using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Rail.Api.Models;

/// <summary>
/// Train entity
/// </summary>
public class Train
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(13)]
    //[Index(IsUnique = true)]
    public string NormalizedIndex { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public ICollection<Wagon> Wagons { get; set; } = new List<Wagon>();
}