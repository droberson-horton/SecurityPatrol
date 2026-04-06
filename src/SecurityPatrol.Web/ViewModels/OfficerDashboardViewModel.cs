using SecurityPatrol.Web.Models;

namespace SecurityPatrol.Web.ViewModels;

public class OfficerDashboardViewModel
{
    public List<AdminMessage> ActiveMessages { get; set; } = new();
    public List<ScheduledPatrol> TodaysScheduledWalks { get; set; } = new();
    public int MissedLocationCount { get; set; }
    public List<MissedLocationItem> MissedLocations { get; set; } = new();
    public List<Patrol> RecentWalks { get; set; } = new();
    public Patrol? ActiveWalk { get; set; }
}

public class MissedLocationItem
{
    public int ScheduledPatrolId { get; set; }
    public DateTime ScheduledDate { get; set; }
    public TimeSpan ScheduledStartTime { get; set; }
    public TimeSpan ScheduledEndTime { get; set; }
    public string OfficerId { get; set; } = string.Empty;
    public string OfficerName { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string BuildingName { get; set; } = string.Empty;
    public string FloorName { get; set; } = string.Empty;
}
