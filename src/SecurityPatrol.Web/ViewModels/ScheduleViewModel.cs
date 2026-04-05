using SecurityPatrol.Web.Models;
using System.ComponentModel.DataAnnotations;

namespace SecurityPatrol.Web.ViewModels;

public class ScheduleViewModel
{
    public List<ScheduledPatrol> Schedules { get; set; } = new();
    public List<ApplicationUser> Officers { get; set; } = new();
    public List<PatrolRoute> Routes { get; set; } = new();
    public ScheduleFormModel Form { get; set; } = new();

    // Filters
    public string? FilterOfficerId { get; set; }
    public DateTime? FilterFromDate { get; set; }
    public DateTime? FilterToDate { get; set; }
    public bool? FilterCompleted { get; set; }
}

public class ScheduleFormModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Officer is required")]
    [Display(Name = "Officer")]
    public string OfficerId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Route is required")]
    [Display(Name = "Walk Route")]
    public int WalkRouteId { get; set; }

    [Required(ErrorMessage = "Date is required")]
    [DataType(DataType.Date)]
    [Display(Name = "Scheduled Date")]
    public DateTime ScheduledDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Start time is required")]
    [Display(Name = "Start Time")]
    public string StartTime { get; set; } = "08:00";

    [Required(ErrorMessage = "End time is required")]
    [Display(Name = "End Time")]
    public string EndTime { get; set; } = "09:00";

    [MaxLength(1000)]
    public string? Notes { get; set; }
}
