namespace SecurityPatrol.Web.ViewModels;

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int ActiveOfficers { get; set; }
    public int TotalBuildings { get; set; }
    public int TotalLocations { get; set; }
    public int TotalRoutes { get; set; }
    public int PatrolsToday { get; set; }
    public int MissedLocationsToday { get; set; }
    public int ActiveMessages { get; set; }
    public int ScheduledPatrolsToday { get; set; }
}
