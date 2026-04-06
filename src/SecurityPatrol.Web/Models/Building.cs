using System.ComponentModel.DataAnnotations;

namespace SecurityPatrol.Web.Models;

public class Building
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Address { get; set; }

    /// <summary>IANA timezone ID (e.g. "America/New_York"). Null falls back to server local time.</summary>
    [MaxLength(100)]
    public string? TimeZoneId { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Floor> Floors { get; set; } = new List<Floor>();
    public ICollection<Location> Locations { get; set; } = new List<Location>();
}
