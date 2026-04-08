using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurityPatrol.Web.Data;
using SecurityPatrol.Web.ViewModels;

namespace SecurityPatrol.Web.Controllers;

[Authorize(Roles = "Administrator,SecurityOfficer")]
public class FloorPlanController : Controller
{
    private readonly ApplicationDbContext _db;

    public FloorPlanController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: /FloorPlan — list of all active floor plans
    public async Task<IActionResult> Index()
    {
        var plans = await _db.FloorPlans
            .Include(fp => fp.Floor)
                .ThenInclude(f => f.Building)
            .Where(fp => fp.IsActive)
            .OrderBy(fp => fp.Floor.Building.Name)
            .ThenBy(fp => fp.Floor.FloorNumber)
            .ThenBy(fp => fp.Label ?? fp.OriginalFileName)
            .ToListAsync();

        var buildings = plans
            .GroupBy(fp => fp.Floor.Building, (b, grp) => new FloorPlanBuildingGroup
            {
                Building = b,
                Floors = grp
                    .GroupBy(fp => fp.Floor, (f, fgrp) => new FloorPlanFloorGroup
                    {
                        Floor = f,
                        Plans = fgrp.ToList()
                    })
                    .ToList()
            })
            .ToList();

        return View(new FloorPlanListViewModel { Buildings = buildings });
    }

    // GET: /FloorPlan/View/5 — read-only map viewer
    public async Task<IActionResult> View(int id)
    {
        var plan = await _db.FloorPlans
            .Include(fp => fp.Floor)
                .ThenInclude(f => f.Building)
            .Include(fp => fp.Locations.Where(l => l.IsActive && l.MapX != null && l.MapY != null))
            .FirstOrDefaultAsync(fp => fp.Id == id && fp.IsActive);

        if (plan == null) return NotFound();

        return View(new FloorPlanViewerViewModel
        {
            FloorPlan = plan,
            Locations = plan.Locations
                .Where(l => l.IsActive && l.MapX.HasValue && l.MapY.HasValue)
                .OrderBy(l => l.PatrolOrder).ThenBy(l => l.Name)
                .ToList()
        });
    }
}
