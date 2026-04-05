using System.ComponentModel.DataAnnotations;

namespace SecurityPatrol.Web.Models;

public class PatrolScanNote
{
    public int Id { get; set; }

    public int PatrolScanId { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Note { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public PatrolScan PatrolScan { get; set; } = null!;
}
