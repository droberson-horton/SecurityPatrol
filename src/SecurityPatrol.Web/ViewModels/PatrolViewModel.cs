using SecurityPatrol.Web.Models;

namespace SecurityPatrol.Web.ViewModels;

public class PatrolStartViewModel
{
    public List<PatrolRoute> AvailableRoutes { get; set; } = new();
    public int? SelectedRouteId { get; set; }
    public int? ScheduledPatrolId { get; set; }
    public List<ScheduledPatrol> TodaysSchedules { get; set; } = new();
}

public class PatrolActiveViewModel
{
    public Patrol Patrol { get; set; } = null!;
    public List<PatrolLocationProgress> LocationProgress { get; set; } = new();
    public int ScannedCount { get; set; }
    public int TotalCount { get; set; }
    public double ProgressPercent => TotalCount > 0 ? (double)ScannedCount / TotalCount * 100 : 0;
    /// <summary>Primary timezone for this patrol (from the route's building).</summary>
    public string? TimeZoneId { get; set; }
}

public class PatrolLocationProgress
{
    public int LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string? FloorName { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public string? TimeZoneId { get; set; }
    public int OrderIndex { get; set; }
    public bool IsScanned { get; set; }
    public DateTime? ScannedAt { get; set; }
    public int? PatrolScanId { get; set; }
}

public class PatrolScanViewModel
{
    public int PatrolId { get; set; }
    public int PatrolScanId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string? FloorName { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public string? TimeZoneId { get; set; }
    public string? ExistingNotes { get; set; }
    public string? PhotoPath { get; set; }
    public DateTime ScannedAt { get; set; }
    public string QrCode { get; set; } = string.Empty;
}

public class PatrolScanInputModel
{
    public int PatrolId { get; set; }
    public string QrCode { get; set; } = string.Empty;
}
