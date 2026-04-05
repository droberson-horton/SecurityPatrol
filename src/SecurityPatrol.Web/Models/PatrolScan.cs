using System.ComponentModel.DataAnnotations;

namespace SecurityPatrol.Web.Models;

public class PatrolScan
{
    public int Id { get; set; }

    public int PatrolId { get; set; }

    public int LocationId { get; set; }

    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [MaxLength(500)]
    public string? PhotoPath { get; set; }

    public Patrol Patrol { get; set; } = null!;
    public Location Location { get; set; } = null!;
    public ICollection<PatrolScanNote> PatrolScanNotes { get; set; } = new List<PatrolScanNote>();
}
