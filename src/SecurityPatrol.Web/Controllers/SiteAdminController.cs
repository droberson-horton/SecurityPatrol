using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurityPatrol.Web.Data;
using SecurityPatrol.Web.Models;
using SecurityPatrol.Web.ViewModels;

namespace SecurityPatrol.Web.Controllers;

[Authorize(Policy = "AdminOnly")]
public class SiteAdminController : Controller
{
    private readonly ApplicationDbContext _db;

    public SiteAdminController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var buildings = await _db.Buildings
            .Include(b => b.Floors.Where(f => f.IsActive).OrderBy(f => f.FloorNumber))
                .ThenInclude(f => f.Locations.Where(l => l.IsActive).OrderBy(l => l.PatrolOrder).ThenBy(l => l.Name))
            .Include(b => b.Floors.Where(f => f.IsActive).OrderBy(f => f.FloorNumber))
                .ThenInclude(f => f.FloorPlans.Where(fp => fp.IsActive))
            .Where(b => b.IsActive)
            .OrderBy(b => b.SortOrder).ThenBy(b => b.Name)
            .ToListAsync();

        ViewBag.TimeZones = TimeZoneInfo.GetSystemTimeZones();
        var vm = new SiteAdminViewModel { BuildingTree = buildings };
        return View("Index", vm);
    }

    // ---- Buildings ----
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBuilding(BuildingFormModel form)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Building name is required.";
            return RedirectToAction(nameof(Index));
        }

        var building = new Building
        {
            Name = form.Name,
            Address = form.Address,
            TimeZoneId = string.IsNullOrEmpty(form.TimeZoneId) ? null : form.TimeZoneId,
            IsActive = form.IsActive,
            CreatedAt = DateTime.UtcNow
        };
        _db.Buildings.Add(building);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Building created.";
        return RedirectToAction(nameof(Index), new { open = building.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditBuilding(BuildingFormModel form)
    {
        var building = await _db.Buildings.FindAsync(form.Id);
        if (building == null) return NotFound();

        building.Name = form.Name;
        building.Address = form.Address;
        building.TimeZoneId = string.IsNullOrEmpty(form.TimeZoneId) ? null : form.TimeZoneId;
        building.IsActive = form.IsActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Building updated.";
        return RedirectToAction(nameof(Index), new { open = building.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBuilding(int id)
    {
        var building = await _db.Buildings.FindAsync(id);
        if (building == null) return NotFound();

        building.IsActive = false;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Building deactivated.";
        return RedirectToAction(nameof(Index));
    }

    // ---- Floors ----
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFloor(FloorFormModel form)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please fill all required fields.";
            return RedirectToAction(nameof(Index));
        }

        var floor = new Floor
        {
            BuildingId = form.BuildingId,
            Name = form.Name,
            FloorNumber = form.FloorNumber,
            IsActive = form.IsActive
        };
        _db.Floors.Add(floor);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Floor created.";
        return RedirectToAction(nameof(Index), new { open = form.BuildingId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditFloor(FloorFormModel form)
    {
        var floor = await _db.Floors.FindAsync(form.Id);
        if (floor == null) return NotFound();

        floor.BuildingId = form.BuildingId;
        floor.Name = form.Name;
        floor.FloorNumber = form.FloorNumber;
        floor.IsActive = form.IsActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Floor updated.";
        return RedirectToAction(nameof(Index), new { open = floor.BuildingId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFloor(int id)
    {
        var floor = await _db.Floors.FindAsync(id);
        if (floor == null) return NotFound();

        var buildingId = floor.BuildingId;
        floor.IsActive = false;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Floor deactivated.";
        return RedirectToAction(nameof(Index), new { open = buildingId });
    }

    // ---- Locations ----
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLocation(LocationFormModel form)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please fill all required fields.";
            return RedirectToAction(nameof(Index));
        }

        var location = new Location
        {
            BuildingId = form.BuildingId,
            FloorId = form.FloorId,
            Name = form.Name,
            Description = form.Description,
            QrCode = Guid.NewGuid().ToString(),
            IsActive = form.IsActive,
            CreatedAt = DateTime.UtcNow
        };
        _db.Locations.Add(location);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Location created.";
        return RedirectToAction(nameof(Index), new { open = form.BuildingId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditLocation(LocationFormModel form)
    {
        var location = await _db.Locations.FindAsync(form.Id);
        if (location == null) return NotFound();

        location.BuildingId = form.BuildingId;
        location.FloorId = form.FloorId;
        location.Name = form.Name;
        location.Description = form.Description;
        location.IsActive = form.IsActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Location updated.";
        return RedirectToAction(nameof(Index), new { open = location.BuildingId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLocation(int id)
    {
        var location = await _db.Locations.FindAsync(id);
        if (location == null) return NotFound();

        var buildingId = location.BuildingId;
        location.IsActive = false;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Location deactivated.";
        return RedirectToAction(nameof(Index), new { open = buildingId });
    }

    // ---- Building Ordering ----
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReorderBuildings([FromBody] List<int> buildingIds)
    {
        if (buildingIds == null || !buildingIds.Any())
            return BadRequest();

        var buildings = await _db.Buildings.Where(b => buildingIds.Contains(b.Id)).ToListAsync();
        for (int i = 0; i < buildingIds.Count; i++)
        {
            var building = buildings.FirstOrDefault(b => b.Id == buildingIds[i]);
            if (building != null)
                building.SortOrder = i;
        }
        await _db.SaveChangesAsync();
        return Ok();
    }

    // ---- Location Patrol Order ----
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReorderLocations([FromBody] List<int> locationIds)
    {
        if (locationIds == null || !locationIds.Any())
            return BadRequest();

        var locations = await _db.Locations.Where(l => locationIds.Contains(l.Id)).ToListAsync();
        for (int i = 0; i < locationIds.Count; i++)
        {
            var loc = locations.FirstOrDefault(l => l.Id == locationIds[i]);
            if (loc != null)
                loc.PatrolOrder = i;
        }
        await _db.SaveChangesAsync();
        return Ok();
    }

    // Ajax endpoint for cascading floor dropdown
    [HttpGet]
    public async Task<IActionResult> GetFloorsByBuilding(int buildingId)
    {
        var floors = await _db.Floors
            .Where(f => f.BuildingId == buildingId && f.IsActive)
            .OrderBy(f => f.FloorNumber)
            .Select(f => new { f.Id, f.Name })
            .ToListAsync();
        return Json(floors);
    }
}
