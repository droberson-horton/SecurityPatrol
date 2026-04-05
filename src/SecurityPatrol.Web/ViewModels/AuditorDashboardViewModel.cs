using SecurityPatrol.Web.Models;

namespace SecurityPatrol.Web.ViewModels;

public class AuditorDashboardViewModel
{
    public int PatrolsToday { get; set; }
    public int MissedLocationCount { get; set; }
    public int CompletedPatrolsToday { get; set; }
    public int ActivePatrolsCount { get; set; }
    public List<MissedLocationItem> MissedLocations { get; set; } = new();
    public List<Building> Buildings { get; set; } = new();
    public List<ApplicationUser> Officers { get; set; } = new();

    // Filter params
    public int? FilterBuildingId { get; set; }
    public int? FilterFloorId { get; set; }
    public int? FilterLocationId { get; set; }
    public string? FilterOfficerId { get; set; }
    public DateTime? FilterFromDate { get; set; }
    public DateTime? FilterToDate { get; set; }
}
