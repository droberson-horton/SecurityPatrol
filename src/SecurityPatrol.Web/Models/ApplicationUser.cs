using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SecurityPatrol.Web.Models;

public enum UserRole
{
    Administrator,
    SecurityOfficer,
    Auditor
}

public class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.SecurityOfficer;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string FullName => $"{FirstName} {LastName}";
}
