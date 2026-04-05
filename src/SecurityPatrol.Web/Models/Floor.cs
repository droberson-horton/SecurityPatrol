using System.ComponentModel.DataAnnotations;

namespace SecurityPatrol.Web.Models;

public class Floor
{
    public int Id { get; set; }

    public int BuildingId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int FloorNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public Building Building { get; set; } = null!;
    public ICollection<Location> Locations { get; set; } = new List<Location>();
}
