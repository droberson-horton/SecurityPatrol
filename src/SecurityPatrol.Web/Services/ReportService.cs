using Microsoft.EntityFrameworkCore;
using SecurityPatrol.Web.Data;
using SecurityPatrol.Web.Models;
using SecurityPatrol.Web.ViewModels;

namespace SecurityPatrol.Web.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _db;
    private readonly IPatrolService _patrolService;

    public ReportService(ApplicationDbContext db, IPatrolService patrolService)
    {
        _db = db;
        _patrolService = patrolService;
    }

    public async Task<HistoryReportViewModel> GetHistoryReportAsync(PatrolFilters filters)
    {
        filters.PageSize = 1000; // Report gets all records
        var (items, total) = await _patrolService.GetPatrolHistoryAsync(filters);

        var officers = await _db.Users
            .Where(u => u.Role == UserRole.SecurityOfficer || u.Role == UserRole.Administrator)
            .OrderBy(u => u.LastName)
            .ToListAsync();

        var allPatrols = await _db.Patrols
            .Include(w => w.ScheduledPatrol)
                .ThenInclude(sw => sw.PatrolRoute)
                    .ThenInclude(r => r.PatrolRouteLocations)
            .Include(w => w.PatrolScans)
            .ToListAsync();

        int totalMissed = 0;
        foreach (var patrol in allPatrols)
        {
            if (patrol.ScheduledPatrol?.PatrolRoute != null)
            {
                var required = patrol.ScheduledPatrol.PatrolRoute.PatrolRouteLocations.Count;
                var scanned = patrol.PatrolScans.Count;
                if (scanned < required)
                    totalMissed += (required - scanned);
            }
        }

        return new HistoryReportViewModel
        {
            Patrols = items,
            TotalPatrols = total,
            CompletedPatrols = items.Count(w => w.Status == PatrolStatus.Completed),
            IncompletePatrols = items.Count(w => w.Status == PatrolStatus.Incomplete),
            InProgressPatrols = items.Count(w => w.Status == PatrolStatus.InProgress),
            TotalLocationsScanned = items.Sum(w => w.ScannedCount),
            TotalMissedLocations = totalMissed,
            FromDate = filters.FromDate,
            ToDate = filters.ToDate,
            OfficerFilter = filters.OfficerId,
            StatusFilter = filters.Status,
            Officers = officers
        };
    }

    public async Task<MissedReportViewModel> GetMissedReportAsync(PatrolFilters filters)
    {
        var missedLocations = await _patrolService.GetMissedLocationsAsync(filters);

        var officers = await _db.Users
            .Where(u => u.Role == UserRole.SecurityOfficer || u.Role == UserRole.Administrator)
            .OrderBy(u => u.LastName)
            .ToListAsync();

        var buildings = await _db.Buildings
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .ToListAsync();

        return new MissedReportViewModel
        {
            MissedLocations = missedLocations,
            FromDate = filters.FromDate,
            ToDate = filters.ToDate,
            TimeFrom = filters.TimeFrom,
            TimeTo = filters.TimeTo,
            OfficerFilter = filters.OfficerId,
            BuildingFilter = filters.BuildingId,
            Officers = officers,
            Buildings = buildings
        };
    }

    public async Task<RoleReportViewModel> GetRoleReportAsync()
    {
        var users = await _db.Users.ToListAsync();
        var walks = await _db.Patrols
            .GroupBy(w => w.OfficerId)
            .Select(g => new
            {
                OfficerId = g.Key,
                Total = g.Count(),
                Completed = g.Count(w => w.Status == PatrolStatus.Completed),
                LastWalk = g.Max(w => (DateTime?)w.StartTime)
            })
            .ToListAsync();

        var walkDict = walks.ToDictionary(w => w.OfficerId);

        var groups = new List<RoleReportGroup>();
        foreach (var role in Enum.GetValues<UserRole>())
        {
            var roleUsers = users.Where(u => u.Role == role).ToList();
            var userItems = roleUsers.Select(u =>
            {
                walkDict.TryGetValue(u.Id, out var stats);
                return new UserReportItem
                {
                    UserId = u.Id,
                    FullName = u.FullName,
                    Email = u.Email ?? "",
                    IsActive = u.IsActive,
                    TotalPatrols = stats?.Total ?? 0,
                    CompletedPatrols = stats?.Completed ?? 0,
                    LastPatrolDate = stats?.LastWalk,
                    CreatedAt = u.CreatedAt
                };
            }).OrderBy(u => u.FullName).ToList();

            groups.Add(new RoleReportGroup { Role = role, Users = userItems });
        }

        return new RoleReportViewModel { Groups = groups };
    }
}
