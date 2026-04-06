using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurityPatrol.Web.Data;
using SecurityPatrol.Web.Models;
using SecurityPatrol.Web.Services;
using SecurityPatrol.Web.ViewModels;
using System.Text;

namespace SecurityPatrol.Web.Controllers;

[Authorize(Policy = "AuditorAndAdmin")]
public class ReportController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IReportService _reportService;
    private readonly IPatrolService _patrolService;

    public ReportController(ApplicationDbContext db, IReportService reportService, IPatrolService patrolService)
    {
        _db = db;
        _reportService = reportService;
        _patrolService = patrolService;
    }

    public async Task<IActionResult> Index()
    {
        var today = DateTime.UtcNow.Date;
        var vm = new ReportIndexViewModel
        {
            TotalPatrols = await _db.Patrols.CountAsync(),
            CompletedPatrols = await _db.Patrols.CountAsync(w => w.Status == PatrolStatus.Completed),
            ActiveOfficers = await _db.Users.CountAsync(u => u.Role == UserRole.SecurityOfficer && u.IsActive)
        };

        var missedFilters = new PatrolFilters { FromDate = today.AddDays(-30), ToDate = today };
        var missed = await _patrolService.GetMissedLocationsAsync(missedFilters);
        vm.MissedLocationsTotal = missed.Count;

        return View(vm);
    }

    public async Task<IActionResult> History(string? officerId, DateTime? fromDate, DateTime? toDate, PatrolStatus? status, bool export = false)
    {
        var filters = new PatrolFilters
        {
            OfficerId = officerId,
            FromDate = fromDate,
            ToDate = toDate,
            Status = status
        };

        var vm = await _reportService.GetHistoryReportAsync(filters);

        if (export)
        {
            return ExportHistoryToCsv(vm);
        }

        return View(vm);
    }

    public async Task<IActionResult> MissedReport(string? officerId, DateTime? fromDate, DateTime? toDate,
        TimeSpan? timeFrom, TimeSpan? timeTo, int? buildingId, bool export = false)
    {
        var filters = new PatrolFilters
        {
            OfficerId = officerId,
            FromDate = fromDate,
            ToDate = toDate,
            TimeFrom = timeFrom,
            TimeTo = timeTo,
            BuildingId = buildingId
        };

        var vm = await _reportService.GetMissedReportAsync(filters);

        if (export)
            return ExportMissedToCsv(vm);

        return View(vm);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var patrol = await _db.Patrols
            .Include(p => p.Officer)
            .Include(p => p.ScheduledPatrol)
                .ThenInclude(sp => sp!.PatrolRoute)
                    .ThenInclude(r => r!.PatrolRouteLocations)
                        .ThenInclude(rl => rl.Location)
                            .ThenInclude(l => l.Floor)
            .Include(p => p.ScheduledPatrol)
                .ThenInclude(sp => sp!.PatrolRoute)
                    .ThenInclude(r => r!.PatrolRouteLocations)
                        .ThenInclude(rl => rl.Location)
                            .ThenInclude(l => l.Building)
            .Include(p => p.PatrolScans)
                .ThenInclude(s => s.Location)
                    .ThenInclude(l => l.Floor)
            .Include(p => p.PatrolScans)
                .ThenInclude(s => s.Location)
                    .ThenInclude(l => l.Building)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (patrol == null) return NotFound();

        var scans = patrol.PatrolScans.OrderBy(s => s.ScannedAt).Select(s => new PatrolScanDetail
        {
            ScanId       = s.Id,
            LocationName = s.Location.Name,
            FloorName    = s.Location.Floor?.Name,
            BuildingName = s.Location.Building.Name,
            ScannedAt    = s.ScannedAt,
            Notes        = s.Notes,
            PhotoPath    = s.PhotoPath
        }).ToList();

        var scannedLocationIds = patrol.PatrolScans.Select(s => s.LocationId).ToHashSet();
        var routeLocations = patrol.ScheduledPatrol?.PatrolRoute?.PatrolRouteLocations
            .OrderBy(rl => rl.OrderIndex)
            .ToList() ?? new();

        var missed = routeLocations
            .Where(rl => !scannedLocationIds.Contains(rl.LocationId))
            .Select(rl => rl.Location.Name)
            .ToList();

        var vm = new PatrolDetailViewModel
        {
            PatrolId             = patrol.Id,
            OfficerName          = patrol.Officer.FullName,
            RouteName            = patrol.ScheduledPatrol?.PatrolRoute?.Name,
            StartTime            = patrol.StartTime,
            EndTime              = patrol.EndTime,
            Status               = patrol.Status,
            Notes                = patrol.Notes,
            Scans                = scans,
            MissedLocationNames  = missed,
            TotalRouteLocations  = routeLocations.Count
        };

        return View(vm);
    }

    public async Task<IActionResult> RoleReport()
    {
        var vm = await _reportService.GetRoleReportAsync();
        return View(vm);
    }

    private FileResult ExportHistoryToCsv(HistoryReportViewModel vm)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Patrol ID,Officer,Route,Start Time,End Time,Duration,Status,Scanned,Total Locations");

        foreach (var patrol in vm.Patrols)
        {
            var duration = patrol.Duration.HasValue ? $"{(int)patrol.Duration.Value.TotalHours}h {patrol.Duration.Value.Minutes}m" : "N/A";
            sb.AppendLine($"{patrol.PatrolId},{EscapeCsv(patrol.OfficerName)},{EscapeCsv(patrol.RouteName ?? "Ad-hoc")},{patrol.StartTime:yyyy-MM-dd HH:mm},{patrol.EndTime?.ToString("yyyy-MM-dd HH:mm") ?? "N/A"},{duration},{patrol.Status},{patrol.ScannedCount},{patrol.TotalLocations}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"patrol-history-{DateTime.Now:yyyyMMdd}.csv");
    }

    private FileResult ExportMissedToCsv(MissedReportViewModel vm)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Scheduled Date,Start Time,End Time,Officer,Route,Building,Floor,Location");

        foreach (var item in vm.MissedLocations)
        {
            sb.AppendLine($"{item.ScheduledDate:yyyy-MM-dd},{item.ScheduledStartTime:hh\\:mm},{item.ScheduledEndTime:hh\\:mm},{EscapeCsv(item.OfficerName)},{EscapeCsv(item.RouteName)},{EscapeCsv(item.BuildingName)},{EscapeCsv(item.FloorName)},{EscapeCsv(item.LocationName)}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"missed-report-{DateTime.Now:yyyyMMdd}.csv");
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
