using SecurityPatrol.Web.Models;
using System.ComponentModel.DataAnnotations;

namespace SecurityPatrol.Web.ViewModels;

public class UserAdminViewModel
{
    public List<ApplicationUser> Users { get; set; } = new();
    public UserFormModel Form { get; set; } = new();
}

public class UserFormModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "First name is required")]
    [MaxLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [MaxLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required")]
    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;

    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    [Display(Name = "Confirm Password")]
    public string? ConfirmPassword { get; set; }
}
