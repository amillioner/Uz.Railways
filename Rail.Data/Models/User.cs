using System;
using System.ComponentModel.DataAnnotations;

namespace Rail.Data.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Roles { get; set; } = string.Empty; // comma-separated roles

    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;

    public string[] GetRoles() => Roles.Split(',', StringSplitOptions.RemoveEmptyEntries);
}