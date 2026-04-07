using System.ComponentModel.DataAnnotations;

namespace SecurityPatrol.Web.Models;

public class FloorPlan
{
    public int Id { get; set; }

    public int FloorId { get; set; }

    [Required]
    [MaxLength(200)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string StoredFileName { get; set; } = string.Empty;

    // "image" or "pdf"
    [MaxLength(10)]
    public string FileType { get; set; } = "image";

    [MaxLength(200)]
    public string? Label { get; set; }

    public int PinRadius { get; set; } = 18;

    public bool IsActive { get; set; } = true;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Floor Floor { get; set; } = null!;

    public ICollection<Location> Locations { get; set; } = new List<Location>();
}
