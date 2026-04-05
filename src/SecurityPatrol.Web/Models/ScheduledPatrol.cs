using System.ComponentModel.DataAnnotations;

namespace SecurityPatrol.Web.Models;

public class ScheduledPatrol
{
    public int Id { get; set; }

    [Required]
    public string OfficerId { get; set; } = string.Empty;

    public int PatrolRouteId { get; set; }

    public DateTime ScheduledDate { get; set; }

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public bool IsCompleted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser Officer { get; set; } = null!;
    public PatrolRoute PatrolRoute { get; set; } = null!;
    public ICollection<Patrol> Patrols { get; set; } = new List<Patrol>();
}
