using System.ComponentModel.DataAnnotations;

namespace SecurityPatrol.Web.Models;

public enum PatrolStatus
{
    InProgress,
    Completed,
    Incomplete
}

public class Patrol
{
    public int Id { get; set; }

    [Required]
    public string OfficerId { get; set; } = string.Empty;

    public int? ScheduledPatrolId { get; set; }

    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    public DateTime? EndTime { get; set; }

    public PatrolStatus Status { get; set; } = PatrolStatus.InProgress;

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public ApplicationUser Officer { get; set; } = null!;
    public ScheduledPatrol? ScheduledPatrol { get; set; }
    public ICollection<PatrolScan> PatrolScans { get; set; } = new List<PatrolScan>();
}
