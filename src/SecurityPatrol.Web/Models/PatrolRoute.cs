using System.ComponentModel.DataAnnotations;

namespace SecurityPatrol.Web.Models;

public class PatrolRoute
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PatrolRouteLocation> PatrolRouteLocations { get; set; } = new List<PatrolRouteLocation>();
}
