using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurityPatrol.Web.Data;
using SecurityPatrol.Web.Models;
using SecurityPatrol.Web.Services;
using SecurityPatrol.Web.ViewModels;

namespace SecurityPatrol.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPatrolService _patrolService;

    public DashboardController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IPatrolService patrolService)
    {
        _db = db;
        _userManager = userManager;
        _patrolService = patrolService;
    }

    [Authorize(Policy = "OfficerAndAdmin")]
    public async Task<IActionResult> Officer()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var activeMessages = await _db.AdminMessages
            .Where(m => m.IsActive && (m.ExpiresAt == null || m.ExpiresAt > DateTime.UtcNow))
            .OrderByDescending(m => m.CreatedAt)
            .Take(5)
            .ToListAsync();

        var todaysSchedules = await _db.ScheduledPatrols
            .Include(sw => sw.PatrolRoute)
            .Where(sw => sw.OfficerId == user.Id && sw.ScheduledDate >= today && sw.ScheduledDate < tomorrow)
            .ToListAsync();

        var recentWalks = await _db.Patrols
            .Include(w => w.ScheduledPatrol)
                .ThenInclude(sw => sw.PatrolRoute)
            .Where(w => w.OfficerId == user.Id)
            .OrderByDescending(w => w.StartTime)
            .Take(5)
            .ToListAsync();

        var activePatrol = await _patrolService.GetActivePatrolAsync(user.Id);

        var missedFilters = new PatrolFilters
        {
            OfficerId = user.Id,
            FromDate = today.AddDays(-7),
            ToDate = today
        };
        var missedLocations = await _patrolService.GetMissedLocationsAsync(missedFilters);

        var vm = new OfficerDashboardViewModel
        {
            ActiveMessages = activeMessages,
            TodaysScheduledWalks = todaysSchedules,
            MissedLocationCount = missedLocations.Count,
            MissedLocations = missedLocations,
            RecentWalks = recentWalks,
            ActiveWalk = activePatrol
        };

        return View(vm);
    }

    [Authorize(Policy = "AuditorAndAdmin")]
    public async Task<IActionResult> Auditor(int? buildingId, int? floorId, int? locationId,
        string? officerId, DateTime? fromDate, DateTime? toDate)
    {
        var today = DateTime.UtcNow.Date;

        var filters = new PatrolFilters
        {
            BuildingId = buildingId,
            FloorId = floorId,
            LocationId = locationId,
            OfficerId = officerId,
            FromDate = fromDate ?? today,
            ToDate = toDate ?? today
        };

        var missedLocations = await _patrolService.GetMissedLocationsAsync(filters);

        var todayStart = today;
        var tomorrowStart = today.AddDays(1);
        var walksToday = await _db.Patrols.CountAsync(w => w.StartTime >= todayStart && w.StartTime < tomorrowStart);
        var completedToday = await _db.Patrols.CountAsync(w => w.StartTime >= todayStart && w.StartTime < tomorrowStart && w.Status == PatrolStatus.Completed);
        var activeWalks = await _db.Patrols.CountAsync(w => w.Status == PatrolStatus.InProgress);

        var buildings = await _db.Buildings.Where(b => b.IsActive).ToListAsync();
        var officers = await _db.Users
            .Where(u => u.Role == UserRole.SecurityOfficer && u.IsActive)
            .OrderBy(u => u.LastName)
            .ToListAsync();

        var vm = new AuditorDashboardViewModel
        {
            PatrolsToday = walksToday,
            MissedLocationCount = missedLocations.Count,
            CompletedPatrolsToday = completedToday,
            ActivePatrolsCount = activeWalks,
            MissedLocations = missedLocations,
            Buildings = buildings,
            Officers = officers,
            FilterBuildingId = buildingId,
            FilterFloorId = floorId,
            FilterLocationId = locationId,
            FilterOfficerId = officerId,
            FilterFromDate = fromDate ?? today,
            FilterToDate = toDate ?? today
        };

        return View(vm);
    }
}
