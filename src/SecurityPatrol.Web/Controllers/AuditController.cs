using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurityPatrol.Web.Data;
using SecurityPatrol.Web.Models;
using SecurityPatrol.Web.Services;
using SecurityPatrol.Web.ViewModels;

namespace SecurityPatrol.Web.Controllers;

[Authorize(Policy = "AuditorAndAdmin")]
public class AuditController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IPatrolService _patrolService;

    public AuditController(ApplicationDbContext db, IPatrolService patrolService)
    {
        _db = db;
        _patrolService = patrolService;
    }

    public async Task<IActionResult> Index(int? buildingId, int? floorId, int? locationId,
        string? officerId, DateTime? fromDate, DateTime? toDate)
    {
        var filters = new PatrolFilters
        {
            BuildingId = buildingId,
            FloorId = floorId,
            LocationId = locationId,
            OfficerId = officerId,
            FromDate = fromDate ?? DateTime.UtcNow.Date.AddDays(-30),
            ToDate = toDate ?? DateTime.UtcNow.Date
        };

        var missedLocations = await _patrolService.GetMissedLocationsAsync(filters);

        var buildings = await _db.Buildings.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();
        var officers = await _db.Users
            .Where(u => u.Role == UserRole.SecurityOfficer && u.IsActive)
            .OrderBy(u => u.LastName)
            .ToListAsync();

        var vm = new AuditorDashboardViewModel
        {
            MissedLocations = missedLocations,
            MissedLocationCount = missedLocations.Count,
            Buildings = buildings,
            Officers = officers,
            FilterBuildingId = buildingId,
            FilterFloorId = floorId,
            FilterLocationId = locationId,
            FilterOfficerId = officerId,
            FilterFromDate = filters.FromDate,
            FilterToDate = filters.ToDate
        };

        return View(vm);
    }

    public async Task<IActionResult> PatrolHistory(int? buildingId, int? floorId, int? locationId,
        string? officerId, DateTime? fromDate, DateTime? toDate, bool missedOnly = false, int page = 1)
    {
        var filters = new PatrolFilters
        {
            BuildingId = buildingId,
            FloorId = floorId,
            LocationId = locationId,
            OfficerId = officerId,
            FromDate = fromDate,
            ToDate = toDate,
            MissedOnly = missedOnly,
            Page = page,
            PageSize = 25
        };

        var (items, total) = await _patrolService.GetPatrolHistoryAsync(filters);

        var officers = await _db.Users
            .Where(u => u.Role == UserRole.SecurityOfficer && u.IsActive)
            .OrderBy(u => u.LastName)
            .ToListAsync();

        var vm = new PatrolHistoryViewModel
        {
            Patrols = items,
            CurrentPage = page,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling((double)total / 25),
            FromDate = fromDate,
            ToDate = toDate,
            OfficerFilter = officerId,
            MissedOnly = missedOnly,
            Officers = officers
        };

        return View(vm);
    }
}
