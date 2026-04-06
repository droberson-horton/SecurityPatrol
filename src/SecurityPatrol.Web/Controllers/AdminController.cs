using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurityPatrol.Web.Data;
using SecurityPatrol.Web.Models;
using SecurityPatrol.Web.Services;
using SecurityPatrol.Web.ViewModels;

namespace SecurityPatrol.Web.Controllers;

[Authorize(Policy = "AdminOnly")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IQrCodeService _qrCodeService;
    private readonly IPatrolService _patrolService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager,
        IQrCodeService qrCodeService, IPatrolService patrolService, ILogger<AdminController> logger)
    {
        _db = db;
        _userManager = userManager;
        _qrCodeService = qrCodeService;
        _patrolService = patrolService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var todayStart = DateTime.UtcNow.Date;
            var todayEnd = todayStart.AddDays(1);
            var vm = new AdminDashboardViewModel
            {
                TotalUsers = await _db.Users.CountAsync(),
                ActiveOfficers = await _db.Users.CountAsync(u => u.Role == UserRole.SecurityOfficer && u.IsActive),
                TotalBuildings = await _db.Buildings.CountAsync(b => b.IsActive),
                TotalLocations = await _db.Locations.CountAsync(l => l.IsActive),
                TotalRoutes = await _db.PatrolRoutes.CountAsync(r => r.IsActive),
                PatrolsToday = await _db.Patrols.CountAsync(w => w.StartTime >= todayStart && w.StartTime < todayEnd),
                ActiveMessages = await _db.AdminMessages.CountAsync(m => m.IsActive && (m.ExpiresAt == null || m.ExpiresAt > DateTime.UtcNow)),
                ScheduledPatrolsToday = await _db.ScheduledPatrols.CountAsync(sw => sw.ScheduledDate >= todayStart && sw.ScheduledDate < todayEnd)
            };

            var missedFilters = new PatrolFilters { FromDate = todayStart, ToDate = todayEnd };
            var missed = await _patrolService.GetMissedLocationsAsync(missedFilters);
            vm.MissedLocationsToday = missed.Count;

            return View(vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading admin dashboard");
            return View(new AdminDashboardViewModel());
        }
    }

    // ---- Messages ----
    public async Task<IActionResult> Messages()
    {
        var messages = await _db.AdminMessages
            .Include(m => m.CreatedBy)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
        return View(messages);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateMessage(string title, string body, DateTime? expiresAt)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
        {
            TempData["Error"] = "Title and body are required.";
            return RedirectToAction(nameof(Messages));
        }

        var message = new AdminMessage
        {
            Title = title,
            Body = body,
            ExpiresAt = expiresAt,
            IsActive = true,
            CreatedById = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        _db.AdminMessages.Add(message);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Message created.";
        return RedirectToAction(nameof(Messages));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditMessage(int id, string title, string body, bool isActive, DateTime? expiresAt)
    {
        var message = await _db.AdminMessages.FindAsync(id);
        if (message == null) return NotFound();

        message.Title = title;
        message.Body = body;
        message.IsActive = isActive;
        message.ExpiresAt = expiresAt;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Message updated.";
        return RedirectToAction(nameof(Messages));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMessage(int id)
    {
        var message = await _db.AdminMessages.FindAsync(id);
        if (message == null) return NotFound();

        _db.AdminMessages.Remove(message);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Message deleted.";
        return RedirectToAction(nameof(Messages));
    }

    // ---- QR Codes ----
    public async Task<IActionResult> QrCodes(int? buildingId, int? floorId, int? locationId)
    {
        var buildings = await _db.Buildings.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();
        var floors = buildingId.HasValue
            ? await _db.Floors.Where(f => f.BuildingId == buildingId && f.IsActive).OrderBy(f => f.FloorNumber).ToListAsync()
            : new List<Floor>();
        var locations = new List<Location>();

        if (buildingId.HasValue || floorId.HasValue || locationId.HasValue)
        {
            locations = await _qrCodeService.GetLocationsForPrintAsync(buildingId, floorId, locationId);
        }

        ViewBag.Buildings = buildings;
        ViewBag.Floors = floors;
        ViewBag.Locations = locations;
        ViewBag.SelectedBuildingId = buildingId;
        ViewBag.SelectedFloorId = floorId;
        ViewBag.SelectedLocationId = locationId;

        return View();
    }

    public async Task<IActionResult> PrintQrCodes(int? buildingId, int? floorId, int? locationId)
    {
        var locations = await _qrCodeService.GetLocationsForPrintAsync(buildingId, floorId, locationId);

        // Encode only the bare location code — keeps QR codes simple and low-density
        // (easier to scan). ScanManual handles both bare codes and full URLs for
        // backwards compatibility with any previously printed codes.
        var qrItems = locations.Select(l => new
        {
            Location = l,
            QrBase64 = _qrCodeService.GenerateQrCode(l.QrCode)
        }).ToList();

        ViewBag.QrItems = qrItems;
        return View();
    }

    // ---- Users ----
    public async Task<IActionResult> Users()
    {
        var users = await _db.Users.OrderBy(u => u.LastName).ToListAsync();
        var vm = new UserAdminViewModel { Users = users };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(UserFormModel form)
    {
        if (!ModelState.IsValid || string.IsNullOrEmpty(form.Password))
        {
            TempData["Error"] = "All fields including password are required when creating a user.";
            return RedirectToAction(nameof(Users));
        }

        var user = new ApplicationUser
        {
            UserName = form.Email,
            Email = form.Email,
            FirstName = form.FirstName,
            LastName = form.LastName,
            Role = form.Role,
            IsActive = form.IsActive,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, form.Password);
        if (result.Succeeded)
        {
            TempData["Success"] = "User created successfully.";
        }
        else
        {
            TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
        }

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(UserFormModel form)
    {
        if (string.IsNullOrEmpty(form.Id))
            return BadRequest();

        var user = await _userManager.FindByIdAsync(form.Id);
        if (user == null) return NotFound();

        user.FirstName = form.FirstName;
        user.LastName = form.LastName;
        user.Email = form.Email;
        user.UserName = form.Email;
        user.NormalizedEmail = form.Email.ToUpper();
        user.NormalizedUserName = form.Email.ToUpper();
        user.Role = form.Role;
        user.IsActive = form.IsActive;

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded && !string.IsNullOrEmpty(form.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _userManager.ResetPasswordAsync(user, token, form.Password);
        }

        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? "User updated."
            : string.Join(", ", result.Errors.Select(e => e.Description));

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser?.Id == id)
        {
            TempData["Error"] = "You cannot delete your own account.";
            return RedirectToAction(nameof(Users));
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        user.IsActive = false;
        await _userManager.UpdateAsync(user);
        TempData["Success"] = "User deactivated.";
        return RedirectToAction(nameof(Users));
    }

    // ---- Schedules ----
    public async Task<IActionResult> Schedules(string? officerId, DateTime? fromDate, DateTime? toDate, bool? completed)
    {
        var query = _db.ScheduledPatrols
            .Include(sw => sw.Officer)
            .Include(sw => sw.PatrolRoute)
            .AsQueryable();

        if (!string.IsNullOrEmpty(officerId))
            query = query.Where(sw => sw.OfficerId == officerId);
        if (fromDate.HasValue)
            query = query.Where(sw => sw.ScheduledDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(sw => sw.ScheduledDate <= toDate.Value);
        if (completed.HasValue)
            query = query.Where(sw => sw.IsCompleted == completed.Value);

        var schedules = await query.OrderByDescending(sw => sw.ScheduledDate).ToListAsync();
        var officers = await _db.Users
            .Where(u => u.Role == UserRole.SecurityOfficer && u.IsActive)
            .OrderBy(u => u.LastName)
            .ToListAsync();
        var routes = await _db.PatrolRoutes.Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();

        var templates = await _db.ScheduleTemplates
            .Include(t => t.Slots)
                .ThenInclude(s => s.PatrolRoute)
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync();

        var vm = new ScheduleViewModel
        {
            Schedules = schedules,
            Officers = officers,
            Routes = routes,
            FilterOfficerId = officerId,
            FilterFromDate = fromDate,
            FilterToDate = toDate,
            FilterCompleted = completed
        };

        ViewBag.Templates = templates;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSchedule(ScheduleFormModel form)
    {
        var schedule = await _db.ScheduledPatrols.FindAsync(form.Id);
        if (schedule == null) return NotFound();

        schedule.OfficerId = form.OfficerId;
        schedule.PatrolRouteId = form.WalkRouteId;
        schedule.ScheduledDate = form.ScheduledDate;
        schedule.StartTime = TimeSpan.Parse(form.StartTime);
        schedule.EndTime = TimeSpan.Parse(form.EndTime);
        schedule.Notes = form.Notes;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Schedule updated.";
        return RedirectToAction(nameof(Schedules));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSchedule(int id)
    {
        var schedule = await _db.ScheduledPatrols.FindAsync(id);
        if (schedule == null) return NotFound();

        _db.ScheduledPatrols.Remove(schedule);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Schedule deleted.";
        return RedirectToAction(nameof(Schedules));
    }

    // ---- Routes ----
    public async Task<IActionResult> Routes()
    {
        var routes = await _db.PatrolRoutes
            .Include(r => r.PatrolRouteLocations)
                .ThenInclude(wrl => wrl.Location)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Name)
            .ToListAsync();
        var vm = new PatrolRouteViewModel { Routes = routes };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRoute(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Route name is required.";
            return RedirectToAction(nameof(Routes));
        }

        var route = new PatrolRoute
        {
            Name = name,
            Description = description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.PatrolRoutes.Add(route);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Route created.";
        return RedirectToAction("EditRoute", new { id = route.Id });
    }

    [HttpGet]
    public async Task<IActionResult> EditRoute(int id)
    {
        var route = await _db.PatrolRoutes
            .Include(r => r.PatrolRouteLocations)
                .ThenInclude(wrl => wrl.Location)
                    .ThenInclude(l => l.Building)
            .Include(r => r.PatrolRouteLocations)
                .ThenInclude(wrl => wrl.Location)
                    .ThenInclude(l => l.Floor)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (route == null) return NotFound();

        var allLocations = await _db.Locations
            .Include(l => l.Building)
            .Include(l => l.Floor)
            .Where(l => l.IsActive)
            .OrderBy(l => l.Building.Name)
            .ThenBy(l => l.Name)
            .ToListAsync();

        var buildings = await _db.Buildings.Where(b => b.IsActive).ToListAsync();

        var form = new PatrolRouteFormModel
        {
            Id = route.Id,
            Name = route.Name,
            Description = route.Description,
            IsActive = route.IsActive,
            Locations = route.PatrolRouteLocations.OrderBy(wrl => wrl.OrderIndex).Select(wrl => new RouteLocationItem
            {
                LocationId = wrl.LocationId,
                LocationName = wrl.Location.Name,
                FloorName = wrl.Location.Floor?.Name,
                BuildingName = wrl.Location.Building.Name,
                OrderIndex = wrl.OrderIndex,
                PatrolRouteLocationId = wrl.Id
            }).ToList()
        };

        var vm = new EditRouteViewModel
        {
            Form = form,
            AllLocations = allLocations,
            Buildings = buildings
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveRoute(int id, string name, string? description, bool isActive, string? locationOrder)
    {
        var route = await _db.PatrolRoutes
            .Include(r => r.PatrolRouteLocations)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (route == null) return NotFound();

        route.Name = name;
        route.Description = description;
        route.IsActive = isActive;

        // Remove all existing route locations
        _db.PatrolRouteLocations.RemoveRange(route.PatrolRouteLocations);

        if (!string.IsNullOrEmpty(locationOrder))
        {
            var locationIds = locationOrder.Split(',').Select(s => int.TryParse(s, out var lid) ? lid : 0).Where(id => id > 0).ToList();
            for (int i = 0; i < locationIds.Count; i++)
            {
                _db.PatrolRouteLocations.Add(new PatrolRouteLocation
                {
                    PatrolRouteId = route.Id,
                    LocationId = locationIds[i],
                    OrderIndex = i + 1
                });
            }
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Route saved.";
        return RedirectToAction(nameof(Routes));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRoute(int id)
    {
        var route = await _db.PatrolRoutes.FindAsync(id);
        if (route == null) return NotFound();

        route.IsActive = false;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Route deactivated.";
        return RedirectToAction(nameof(Routes));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReorderRoutes([FromBody] List<int> routeIds)
    {
        for (int i = 0; i < routeIds.Count; i++)
        {
            var route = await _db.PatrolRoutes.FindAsync(routeIds[i]);
            if (route != null)
                route.SortOrder = i;
        }
        await _db.SaveChangesAsync();
        return Ok();
    }

    // ---- Schedule Templates ----
    public async Task<IActionResult> ScheduleTemplates()
    {
        var templates = await _db.ScheduleTemplates
            .Include(t => t.Slots)
                .ThenInclude(s => s.PatrolRoute)
            .OrderBy(t => t.Name)
            .ToListAsync();

        var routes = await _db.PatrolRoutes.Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();
        ViewBag.Routes = routes;

        return View(templates);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTemplate(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Template name is required.";
            return RedirectToAction(nameof(ScheduleTemplates));
        }

        var template = new ScheduleTemplate
        {
            Name = name,
            Description = description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.ScheduleTemplates.Add(template);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Template created.";
        return RedirectToAction(nameof(EditTemplate), new { id = template.Id });
    }

    [HttpGet]
    public async Task<IActionResult> EditTemplate(int id)
    {
        var template = await _db.ScheduleTemplates
            .Include(t => t.Slots)
                .ThenInclude(s => s.PatrolRoute)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template == null) return NotFound();

        var routes = await _db.PatrolRoutes.Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();
        ViewBag.Routes = routes;

        return View(template);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTemplate(int id, string name, string? description, bool isActive)
    {
        var template = await _db.ScheduleTemplates.FindAsync(id);
        if (template == null) return NotFound();

        template.Name = name;
        template.Description = description;
        template.IsActive = isActive;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Template saved.";
        return RedirectToAction(nameof(EditTemplate), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTemplateSlot(int templateId, string label, string startTime, string endTime, int? patrolRouteId)
    {
        var template = await _db.ScheduleTemplates.FindAsync(templateId);
        if (template == null) return NotFound();

        var maxOrder = await _db.ScheduleTemplateSlots
            .Where(s => s.TemplateId == templateId)
            .Select(s => (int?)s.OrderIndex)
            .MaxAsync() ?? 0;

        var slot = new ScheduleTemplateSlot
        {
            TemplateId = templateId,
            Label = label,
            StartTime = TimeSpan.Parse(startTime),
            EndTime = TimeSpan.Parse(endTime),
            PatrolRouteId = patrolRouteId,
            OrderIndex = maxOrder + 1
        };

        _db.ScheduleTemplateSlots.Add(slot);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Slot added.";
        return RedirectToAction(nameof(EditTemplate), new { id = templateId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTemplateSlot(int slotId, int templateId, string label, string startTime, string endTime, int? patrolRouteId)
    {
        var slot = await _db.ScheduleTemplateSlots.FindAsync(slotId);
        if (slot == null) return NotFound();

        slot.Label = label;
        slot.StartTime = TimeSpan.Parse(startTime);
        slot.EndTime = TimeSpan.Parse(endTime);
        slot.PatrolRouteId = patrolRouteId;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Slot updated.";
        return RedirectToAction(nameof(EditTemplate), new { id = templateId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTemplateSlot(int slotId, int templateId)
    {
        var slot = await _db.ScheduleTemplateSlots.FindAsync(slotId);
        if (slot == null) return NotFound();

        _db.ScheduleTemplateSlots.Remove(slot);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Slot deleted.";
        return RedirectToAction(nameof(EditTemplate), new { id = templateId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTemplate(int id)
    {
        var template = await _db.ScheduleTemplates.FindAsync(id);
        if (template == null) return NotFound();

        template.IsActive = false;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Template deactivated.";
        return RedirectToAction(nameof(ScheduleTemplates));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSchedule(ScheduleFormModel form, int? templateId, DateTime? schedFromDate, DateTime? schedToDate)
    {
        if (templateId.HasValue)
        {
            var template = await _db.ScheduleTemplates
                .Include(t => t.Slots)
                .FirstOrDefaultAsync(t => t.Id == templateId.Value);

            if (template == null)
            {
                TempData["Error"] = "Template not found.";
                return RedirectToAction(nameof(Schedules));
            }

            if (string.IsNullOrEmpty(form.OfficerId) || schedFromDate == null || schedToDate == null)
            {
                TempData["Error"] = "Officer and date range are required when using a template.";
                return RedirectToAction(nameof(Schedules));
            }

            var fromDate = schedFromDate.Value.Date;
            var toDate = schedToDate.Value.Date;

            if (toDate < fromDate)
            {
                TempData["Error"] = "End date must be on or after start date.";
                return RedirectToAction(nameof(Schedules));
            }

            var activeSlots = template.Slots.Where(s => s.PatrolRouteId != null).OrderBy(s => s.OrderIndex).ToList();
            int created = 0;

            for (var date = fromDate; date <= toDate; date = date.AddDays(1))
            {
                foreach (var slot in activeSlots)
                {
                    _db.ScheduledPatrols.Add(new ScheduledPatrol
                    {
                        OfficerId = form.OfficerId,
                        PatrolRouteId = slot.PatrolRouteId!.Value,
                        ScheduledDate = date,
                        StartTime = slot.StartTime,
                        EndTime = slot.EndTime,
                        Notes = slot.Label,
                        CreatedAt = DateTime.UtcNow
                    });
                    created++;
                }
            }

            await _db.SaveChangesAsync();
            int days = (int)(toDate - fromDate).TotalDays + 1;
            TempData["Success"] = $"Created {created} schedule(s) from template '{template.Name}' across {days} day(s).";
            return RedirectToAction(nameof(Schedules));
        }

        if (string.IsNullOrEmpty(form.OfficerId) || form.WalkRouteId == 0 || schedFromDate == null || schedToDate == null)
        {
            TempData["Error"] = "Officer, route, and date range are required.";
            return RedirectToAction(nameof(Schedules));
        }

        var manualFrom = schedFromDate.Value.Date;
        var manualTo   = schedToDate.Value.Date;

        if (manualTo < manualFrom)
        {
            TempData["Error"] = "End date must be on or after start date.";
            return RedirectToAction(nameof(Schedules));
        }

        var startTime    = TimeSpan.Parse(form.StartTime);
        var endTime      = TimeSpan.Parse(form.EndTime);
        int manualCreated = 0;

        for (var date = manualFrom; date <= manualTo; date = date.AddDays(1))
        {
            _db.ScheduledPatrols.Add(new ScheduledPatrol
            {
                OfficerId     = form.OfficerId,
                PatrolRouteId = form.WalkRouteId,
                ScheduledDate = date,
                StartTime     = startTime,
                EndTime       = endTime,
                Notes         = form.Notes,
                CreatedAt     = DateTime.UtcNow
            });
            manualCreated++;
        }

        await _db.SaveChangesAsync();
        int manualDays = (int)(manualTo - manualFrom).TotalDays + 1;
        TempData["Success"] = manualDays == 1
            ? "Schedule created."
            : $"Created {manualCreated} schedule(s) across {manualDays} day(s).";
        return RedirectToAction(nameof(Schedules));
    }
}
