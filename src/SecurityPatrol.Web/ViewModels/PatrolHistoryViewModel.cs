using SecurityPatrol.Web.Models;

namespace SecurityPatrol.Web.ViewModels;

public class PatrolHistoryViewModel
{
    public List<PatrolHistoryItem> Patrols { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public int PageSize { get; set; } = 20;

    // Filters
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public PatrolStatus? StatusFilter { get; set; }
    public string? OfficerFilter { get; set; }
    public List<ApplicationUser> Officers { get; set; } = new();
    public bool MissedOnly { get; set; }
}

public class PatrolHistoryItem
{
    public int PatrolId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public PatrolStatus Status { get; set; }
    public string OfficerName { get; set; } = string.Empty;
    public string? RouteName { get; set; }
    public string? TimeZoneId { get; set; }
    public int ScannedCount { get; set; }
    public int TotalLocations { get; set; }
    public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;
    public List<PatrolScanDetail> Scans { get; set; } = new();
}

public class PatrolScanDetail
{
    public int ScanId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string? FloorName { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public string? TimeZoneId { get; set; }
    public DateTime ScannedAt { get; set; }
    public string? Notes { get; set; }
    public string? PhotoPath { get; set; }
}
