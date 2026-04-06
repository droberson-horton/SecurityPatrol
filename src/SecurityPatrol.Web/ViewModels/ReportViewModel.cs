using SecurityPatrol.Web.Models;

namespace SecurityPatrol.Web.ViewModels;

public class MissedReportViewModel
{
    public List<MissedLocationItem> MissedLocations { get; set; } = new();

    // Filters
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public TimeSpan? TimeFrom { get; set; }
    public TimeSpan? TimeTo { get; set; }
    public string? OfficerFilter { get; set; }
    public int? BuildingFilter { get; set; }
    public List<ApplicationUser> Officers { get; set; } = new();
    public List<Building> Buildings { get; set; } = new();
}

public class PatrolDetailViewModel
{
    public int PatrolId { get; set; }
    public string OfficerName { get; set; } = string.Empty;
    public string? RouteName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public PatrolStatus Status { get; set; }
    public string? Notes { get; set; }
    public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;

    public List<PatrolScanDetail> Scans { get; set; } = new();
    public List<string> MissedLocationNames { get; set; } = new();
    public int TotalRouteLocations { get; set; }
}

public class ReportIndexViewModel
{
    public int TotalPatrols { get; set; }
    public int CompletedPatrols { get; set; }
    public int MissedLocationsTotal { get; set; }
    public int ActiveOfficers { get; set; }
}

public class HistoryReportViewModel
{
    public List<PatrolHistoryItem> Patrols { get; set; } = new();
    public int TotalPatrols { get; set; }
    public int CompletedPatrols { get; set; }
    public int IncompletePatrols { get; set; }
    public int InProgressPatrols { get; set; }
    public int TotalLocationsScanned { get; set; }
    public int TotalMissedLocations { get; set; }

    // Filters
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? OfficerFilter { get; set; }
    public PatrolStatus? StatusFilter { get; set; }
    public List<ApplicationUser> Officers { get; set; } = new();
}

public class RoleReportViewModel
{
    public List<RoleReportGroup> Groups { get; set; } = new();
}

public class RoleReportGroup
{
    public UserRole Role { get; set; }
    public List<UserReportItem> Users { get; set; } = new();
}

public class UserReportItem
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int TotalPatrols { get; set; }
    public int CompletedPatrols { get; set; }
    public DateTime? LastPatrolDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
