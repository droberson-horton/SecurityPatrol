using Microsoft.EntityFrameworkCore;
using SecurityPatrol.Web.Data;
using SecurityPatrol.Web.Models;
using SecurityPatrol.Web.ViewModels;

namespace SecurityPatrol.Web.Services;

public class PatrolService : IPatrolService
{
    private readonly ApplicationDbContext _db;

    public PatrolService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<MissedLocationItem>> GetMissedLocationsAsync(PatrolFilters filters)
    {
        var query = _db.ScheduledPatrols
            .Include(sw => sw.Officer)
            .Include(sw => sw.PatrolRoute)
                .ThenInclude(r => r.PatrolRouteLocations)
                    .ThenInclude(wrl => wrl.Location)
                        .ThenInclude(l => l.Building)
            .Include(sw => sw.PatrolRoute)
                .ThenInclude(r => r.PatrolRouteLocations)
                    .ThenInclude(wrl => wrl.Location)
                        .ThenInclude(l => l.Floor)
            .Include(sw => sw.Patrols)
                .ThenInclude(w => w.PatrolScans)
            .AsQueryable();

        if (!string.IsNullOrEmpty(filters.OfficerId))
            query = query.Where(sw => sw.OfficerId == filters.OfficerId);

        if (filters.FromDate.HasValue)
            query = query.Where(sw => sw.ScheduledDate >= filters.FromDate.Value);

        if (filters.ToDate.HasValue)
            query = query.Where(sw => sw.ScheduledDate <= filters.ToDate.Value);

        var scheduledPatrols = await query.ToListAsync();

        var missed = new List<MissedLocationItem>();

        var now = DateTime.UtcNow;

        foreach (var sw in scheduledPatrols)
        {
            if (sw.PatrolRoute == null)
                continue;

            // Only count missed locations after the scheduled end time has passed
            if (sw.ScheduledDate.Date + sw.EndTime > now)
                continue;

            // Filter by time-of-day range if specified
            if (filters.TimeFrom.HasValue && sw.StartTime < filters.TimeFrom.Value)
                continue;
            if (filters.TimeTo.HasValue && sw.StartTime > filters.TimeTo.Value)
                continue;

            var scannedLocationIds = sw.Patrols
                .SelectMany(w => w.PatrolScans)
                .Select(ws => ws.LocationId)
                .ToHashSet();

            foreach (var wrl in sw.PatrolRoute.PatrolRouteLocations)
            {
                if (!scannedLocationIds.Contains(wrl.LocationId))
                {
                    if (filters.BuildingId.HasValue && wrl.Location.BuildingId != filters.BuildingId.Value)
                        continue;
                    if (filters.FloorId.HasValue && wrl.Location.FloorId != filters.FloorId.Value)
                        continue;
                    if (filters.LocationId.HasValue && wrl.LocationId != filters.LocationId.Value)
                        continue;

                    missed.Add(new MissedLocationItem
                    {
                        ScheduledPatrolId = sw.Id,
                        ScheduledDate = sw.ScheduledDate,
                        ScheduledStartTime = sw.StartTime,
                        ScheduledEndTime = sw.EndTime,
                        OfficerId = sw.OfficerId,
                        OfficerName = sw.Officer.FullName,
                        RouteName = sw.PatrolRoute.Name,
                        LocationName = wrl.Location.Name,
                        BuildingName = wrl.Location.Building.Name,
                        FloorName = wrl.Location.Floor?.Name ?? "N/A"
                    });
                }
            }
        }

        return missed;
    }

    public async Task<PatrolActiveViewModel> GetPatrolProgressAsync(int patrolId)
    {
        var patrol = await _db.Patrols
            .Include(w => w.Officer)
            .Include(w => w.ScheduledPatrol)
            .Include(w => w.PatrolScans)
                .ThenInclude(ws => ws.Location)
                    .ThenInclude(l => l.Building)
            .Include(w => w.PatrolScans)
                .ThenInclude(ws => ws.Location)
                    .ThenInclude(l => l.Floor)
            .FirstOrDefaultAsync(w => w.Id == patrolId);

        if (patrol == null)
            return new PatrolActiveViewModel();

        var vm = new PatrolActiveViewModel { Patrol = patrol };
        var scannedIds = patrol.PatrolScans.ToDictionary(ws => ws.LocationId, ws => ws);

        if (patrol.ScheduledPatrolId.HasValue)
        {
            var scheduledPatrol = await _db.ScheduledPatrols
                .FirstOrDefaultAsync(sw => sw.Id == patrol.ScheduledPatrolId.Value);

            if (scheduledPatrol != null)
            {
                var routeLocations = await _db.PatrolRouteLocations
                    .Include(wrl => wrl.Location)
                        .ThenInclude(l => l.Building)
                    .Include(wrl => wrl.Location)
                        .ThenInclude(l => l.Floor)
                    .Where(wrl => wrl.PatrolRouteId == scheduledPatrol.PatrolRouteId)
                    .OrderBy(wrl => wrl.OrderIndex)
                    .ToListAsync();

                foreach (var wrl in routeLocations)
                {
                    var isScanned = scannedIds.TryGetValue(wrl.LocationId, out var scan);
                    vm.LocationProgress.Add(new PatrolLocationProgress
                    {
                        LocationId = wrl.LocationId,
                        LocationName = wrl.Location.Name,
                        FloorName = wrl.Location.Floor?.Name,
                        BuildingName = wrl.Location.Building.Name,
                        TimeZoneId = wrl.Location.Building.TimeZoneId,
                        OrderIndex = wrl.OrderIndex,
                        IsScanned = isScanned,
                        ScannedAt = scan?.ScannedAt,
                        PatrolScanId = scan?.Id
                    });
                }
                vm.TimeZoneId = routeLocations.FirstOrDefault()?.Location.Building.TimeZoneId;
            }
        }
        else
        {
            // Ad-hoc patrol - show scanned locations
            foreach (var ws in patrol.PatrolScans.OrderBy(s => s.ScannedAt))
            {
                vm.LocationProgress.Add(new PatrolLocationProgress
                {
                    LocationId = ws.LocationId,
                    LocationName = ws.Location.Name,
                    FloorName = ws.Location.Floor?.Name,
                    BuildingName = ws.Location.Building.Name,
                    TimeZoneId = ws.Location.Building.TimeZoneId,
                    OrderIndex = 0,
                    IsScanned = true,
                    ScannedAt = ws.ScannedAt,
                    PatrolScanId = ws.Id
                });
            }
            vm.TimeZoneId = patrol.PatrolScans.FirstOrDefault()?.Location.Building.TimeZoneId;
        }

        vm.ScannedCount = vm.LocationProgress.Count(lp => lp.IsScanned);
        vm.TotalCount = vm.LocationProgress.Count;

        return vm;
    }

    public async Task<Location?> ValidateQrCodeAsync(string qrCode)
    {
        return await _db.Locations
            .Include(l => l.Building)
            .Include(l => l.Floor)
            .FirstOrDefaultAsync(l => l.QrCode == qrCode && l.IsActive);
    }

    public async Task<(List<PatrolHistoryItem> items, int totalCount)> GetPatrolHistoryAsync(PatrolFilters filters)
    {
        var query = _db.Patrols
            .Include(w => w.Officer)
            .Include(w => w.ScheduledPatrol)
            .Include(w => w.PatrolScans)
                .ThenInclude(ws => ws.Location)
                    .ThenInclude(l => l.Building)
            .Include(w => w.PatrolScans)
                .ThenInclude(ws => ws.Location)
                    .ThenInclude(l => l.Floor)
            .AsQueryable();

        if (!string.IsNullOrEmpty(filters.OfficerId))
            query = query.Where(w => w.OfficerId == filters.OfficerId);

        if (filters.FromDate.HasValue)
            query = query.Where(w => w.StartTime >= filters.FromDate.Value);

        if (filters.ToDate.HasValue)
            query = query.Where(w => w.StartTime <= filters.ToDate.Value.AddDays(1));

        if (filters.Status.HasValue)
            query = query.Where(w => w.Status == filters.Status.Value);

        query = query.OrderByDescending(w => w.StartTime);

        var totalCount = await query.CountAsync();

        var patrols = await query
            .Skip((filters.Page - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .ToListAsync();

        // Load route info separately for patrols with scheduled patrol
        var scheduledPatrolIds = patrols
            .Where(w => w.ScheduledPatrolId.HasValue)
            .Select(w => w.ScheduledPatrolId!.Value)
            .Distinct()
            .ToList();

        var scheduledPatrolsWithRoute = new Dictionary<int, ScheduledPatrol>();
        if (scheduledPatrolIds.Any())
        {
            var loadedScheduled = await _db.ScheduledPatrols
                .Include(sw => sw.PatrolRoute)
                    .ThenInclude(r => r.PatrolRouteLocations)
                .Where(sw => scheduledPatrolIds.Contains(sw.Id))
                .ToListAsync();
            scheduledPatrolsWithRoute = loadedScheduled.ToDictionary(sw => sw.Id);
        }

        var items = new List<PatrolHistoryItem>();

        foreach (var patrol in patrols)
        {
            int totalLocations = 0;
            string? routeName = null;

            if (patrol.ScheduledPatrolId.HasValue && scheduledPatrolsWithRoute.TryGetValue(patrol.ScheduledPatrolId.Value, out var sw))
            {
                totalLocations = sw.PatrolRoute?.PatrolRouteLocations.Count ?? 0;
                routeName = sw.PatrolRoute?.Name;
            }

            if (filters.MissedOnly && patrol.PatrolScans.Count >= totalLocations && totalLocations > 0)
                continue;

            var item = new PatrolHistoryItem
            {
                PatrolId = patrol.Id,
                StartTime = patrol.StartTime,
                EndTime = patrol.EndTime,
                Status = patrol.Status,
                OfficerName = patrol.Officer.FullName,
                RouteName = routeName,
                ScannedCount = patrol.PatrolScans.Count,
                TotalLocations = totalLocations > 0 ? totalLocations : patrol.PatrolScans.Count
            };

            foreach (var ws in patrol.PatrolScans.OrderBy(s => s.ScannedAt))
            {
                item.Scans.Add(new PatrolScanDetail
                {
                    ScanId = ws.Id,
                    LocationName = ws.Location.Name,
                    FloorName = ws.Location.Floor?.Name,
                    BuildingName = ws.Location.Building.Name,
                    TimeZoneId = ws.Location.Building.TimeZoneId,
                    ScannedAt = ws.ScannedAt,
                    Notes = ws.Notes,
                    PhotoPath = ws.PhotoPath
                });
            }
            item.TimeZoneId = patrol.PatrolScans.FirstOrDefault()?.Location.Building.TimeZoneId;

            items.Add(item);
        }

        return (items, totalCount);
    }

    public async Task<Patrol?> GetActivePatrolAsync(string officerId)
    {
        return await _db.Patrols
            .Include(w => w.ScheduledPatrol)
            .FirstOrDefaultAsync(w => w.OfficerId == officerId && w.Status == PatrolStatus.InProgress);
    }

    public async Task<Patrol> StartPatrolAsync(string officerId, int patrolRouteId, int? scheduledPatrolId)
    {
        var patrol = new Patrol
        {
            OfficerId = officerId,
            ScheduledPatrolId = scheduledPatrolId,
            StartTime = DateTime.UtcNow,
            Status = PatrolStatus.InProgress
        };

        if (!scheduledPatrolId.HasValue)
        {
            // Ad-hoc: create a scheduled patrol linked to the route
            var scheduledPatrol = new ScheduledPatrol
            {
                OfficerId = officerId,
                PatrolRouteId = patrolRouteId,
                ScheduledDate = DateTime.UtcNow.Date,
                StartTime = DateTime.UtcNow.TimeOfDay,
                EndTime = DateTime.UtcNow.TimeOfDay.Add(TimeSpan.FromHours(1)),
                CreatedAt = DateTime.UtcNow
            };
            _db.ScheduledPatrols.Add(scheduledPatrol);
            await _db.SaveChangesAsync();
            patrol.ScheduledPatrolId = scheduledPatrol.Id;
        }

        _db.Patrols.Add(patrol);
        await _db.SaveChangesAsync();
        return patrol;
    }

    public async Task<PatrolScan> RecordScanAsync(int patrolId, int locationId)
    {
        // Check if already scanned
        var existing = await _db.PatrolScans
            .FirstOrDefaultAsync(ws => ws.PatrolId == patrolId && ws.LocationId == locationId);

        if (existing != null)
            return existing;

        var scan = new PatrolScan
        {
            PatrolId = patrolId,
            LocationId = locationId,
            ScannedAt = DateTime.UtcNow
        };

        _db.PatrolScans.Add(scan);
        await _db.SaveChangesAsync();
        return scan;
    }

    public async Task EndPatrolAsync(int patrolId, PatrolStatus status)
    {
        var patrol = await _db.Patrols
            .Include(w => w.PatrolScans)
            .FirstOrDefaultAsync(w => w.Id == patrolId);

        if (patrol == null) return;

        patrol.EndTime = DateTime.UtcNow;

        int requiredCount = 0;
        if (patrol.ScheduledPatrolId.HasValue)
        {
            var sw = await _db.ScheduledPatrols
                .Include(s => s.PatrolRoute)
                    .ThenInclude(r => r.PatrolRouteLocations)
                .FirstOrDefaultAsync(s => s.Id == patrol.ScheduledPatrolId.Value);

            if (sw?.PatrolRoute != null)
            {
                requiredCount = sw.PatrolRoute.PatrolRouteLocations.Count;
                if (patrol.PatrolScans.Count >= requiredCount)
                {
                    patrol.Status = PatrolStatus.Completed;
                    sw.IsCompleted = true;
                }
                else
                {
                    patrol.Status = PatrolStatus.Incomplete;
                }
            }
            else
            {
                patrol.Status = status;
            }
        }
        else
        {
            patrol.Status = status;
        }

        await _db.SaveChangesAsync();
    }
}
