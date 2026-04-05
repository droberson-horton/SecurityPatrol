using System.ComponentModel.DataAnnotations;

namespace SecurityPatrol.Web.Models;

public class AdminMessage
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(4000)]
    public string Body { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public string CreatedById { get; set; } = string.Empty;

    public DateTime? ExpiresAt { get; set; }

    public ApplicationUser CreatedBy { get; set; } = null!;
}
