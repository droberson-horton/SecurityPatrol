using System.ComponentModel.DataAnnotations;

namespace SecurityPatrol.Web.Models;

public class Location
{
    public int Id { get; set; }

    public int BuildingId { get; set; }

    public int? FloorId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(200)]
    public string QrCode { get; set; } = Guid.NewGuid().ToString();

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Floor plan pin coordinates (0.0–1.0 as fraction of image dimensions)
    public int? FloorPlanId { get; set; }
    public double? MapX { get; set; }
    public double? MapY { get; set; }

    public Building Building { get; set; } = null!;
    public Floor? Floor { get; set; }
    public FloorPlan? FloorPlan { get; set; }
}
