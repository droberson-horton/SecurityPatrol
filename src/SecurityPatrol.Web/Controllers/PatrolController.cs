using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurityPatrol.Web.Data;
using SecurityPatrol.Web.Models;
using SecurityPatrol.Web.Services;
using SecurityPatrol.Web.ViewModels;

namespace SecurityPatrol.Web.Controllers;

[Authorize(Policy = "OfficerAndAdmin")]
public class PatrolController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPatrolService _patrolService;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;

    public PatrolController(ApplicationDbContext db, UserManager<ApplicationUser> userManager,
        IPatrolService patrolService, IWebHostEnvironment env, IConfiguration config)
    {
        _db = db;
        _userManager = userManager;
        _patrolService = patrolService;
        _env = env;
        _config = config;
    }

    [HttpGet]
    public async Task<IActionResult> Start()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var activePatrol = await _patrolService.GetActivePatrolAsync(user.Id);
        if (activePatrol != null)
            return RedirectToAction("Active", new { patrolId = activePatrol.Id });

        var routes = await _db.PatrolRoutes
            .Where(r => r.IsActive)
            .Include(r => r.PatrolRouteLocations)
                .ThenInclude(wrl => wrl.Location)
            .OrderBy(r => r.Name)
            .ToListAsync();

        var today = DateTime.UtcNow.Date;
        var todaysSchedules = await _db.ScheduledPatrols
            .Include(sw => sw.PatrolRoute)
            .Where(sw => sw.OfficerId == user.Id && sw.ScheduledDate.Date == today && !sw.IsCompleted)
            .ToListAsync();

        var vm = new PatrolStartViewModel
        {
            AvailableRoutes = routes,
            TodaysSchedules = todaysSchedules
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int patrolRouteId, int? scheduledPatrolId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var route = await _db.PatrolRoutes.FindAsync(patrolRouteId);
        if (route == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid patrol route selected.");
            return RedirectToAction(nameof(Start));
        }

        var patrol = await _patrolService.StartPatrolAsync(user.Id, patrolRouteId, scheduledPatrolId);
        return RedirectToAction("Active", new { patrolId = patrol.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Active(int patrolId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var patrol = await _db.Patrols.FirstOrDefaultAsync(w => w.Id == patrolId);
        if (patrol == null || patrol.OfficerId != user.Id)
            return NotFound();

        if (patrol.Status != PatrolStatus.InProgress)
            return RedirectToAction(nameof(History));

        var vm = await _patrolService.GetPatrolProgressAsync(patrolId);
        return View(vm);
    }

    [HttpGet]
    [Route("Patrol/Scan/{qrCode}")]
    public async Task<IActionResult> Scan(string qrCode, int patrolId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        Patrol? patrol = null;
        if (patrolId > 0)
        {
            patrol = await _db.Patrols.FirstOrDefaultAsync(w => w.Id == patrolId && w.OfficerId == user.Id && w.Status == PatrolStatus.InProgress);
        }
        else
        {
            // Try to find an active patrol for this officer
            patrol = await _patrolService.GetActivePatrolAsync(user.Id);
        }

        if (patrol == null)
        {
            TempData["Error"] = "No active patrol found. Please start a patrol first.";
            return RedirectToAction("Start");
        }

        var location = await _patrolService.ValidateQrCodeAsync(qrCode);
        if (location == null)
        {
            TempData["Error"] = "Invalid QR code. Location not found.";
            return RedirectToAction("Active", new { patrolId });
        }

        // Validate that this location belongs to the patrol route
        if (patrol.ScheduledPatrolId.HasValue)
        {
            var scheduledPatrol = await _db.ScheduledPatrols
                .Include(sw => sw.PatrolRoute)
                    .ThenInclude(r => r.PatrolRouteLocations)
                .FirstOrDefaultAsync(sw => sw.Id == patrol.ScheduledPatrolId.Value);

            if (scheduledPatrol != null)
            {
                var isInRoute = scheduledPatrol.PatrolRoute.PatrolRouteLocations.Any(wrl => wrl.LocationId == location.Id);
                if (!isInRoute)
                {
                    TempData["Warning"] = $"Location '{location.Name}' is not part of this route, but scan was recorded.";
                }
            }
        }

        var scan = await _patrolService.RecordScanAsync(patrolId, location.Id);

        var vm = new PatrolScanViewModel
        {
            PatrolId = patrolId,
            PatrolScanId = scan.Id,
            LocationName = location.Name,
            FloorName = location.Floor?.Name,
            BuildingName = location.Building.Name,
            TimeZoneId = location.Building.TimeZoneId,
            ExistingNotes = scan.Notes,
            PhotoPath = scan.PhotoPath,
            ScannedAt = scan.ScannedAt,
            QrCode = qrCode
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ScanManual(int patrolId, string qrCode)
    {
        // If the scanned value is a full URL (e.g. from a QR code printed by the app),
        // extract just the code segment at the end of the path.
        if (Uri.TryCreate(qrCode?.Trim(), UriKind.Absolute, out var uri))
        {
            qrCode = uri.Segments.LastOrDefault()?.Trim('/') ?? qrCode;
        }
        return RedirectToAction("Scan", new { qrCode, patrolId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Note(int patrolScanId, string note, int patrolId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var scan = await _db.PatrolScans
            .Include(ws => ws.Patrol)
            .FirstOrDefaultAsync(ws => ws.Id == patrolScanId);

        if (scan == null || scan.Patrol.OfficerId != user.Id)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(note))
        {
            scan.Notes = note;
            var scanNote = new PatrolScanNote
            {
                PatrolScanId = patrolScanId,
                Note = note,
                CreatedAt = DateTime.UtcNow
            };
            _db.PatrolScanNotes.Add(scanNote);
            await _db.SaveChangesAsync();
        }

        return RedirectToAction("Active", new { patrolId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPhoto(int patrolScanId, IFormFile photo, int patrolId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var scan = await _db.PatrolScans
            .Include(ws => ws.Patrol)
            .FirstOrDefaultAsync(ws => ws.Id == patrolScanId);

        if (scan == null || scan.Patrol.OfficerId != user.Id)
            return NotFound();

        if (photo != null && photo.Length > 0)
        {
            var maxMb = _config.GetValue<int>("AppSettings:MaxPhotoSizeMB", 5);
            if (photo.Length > maxMb * 1024 * 1024)
            {
                TempData["Error"] = $"Photo exceeds maximum size of {maxMb}MB.";
                return RedirectToAction("Scan", new { qrCode = scan.PatrolId, patrolId });
            }

            var uploadDir = Path.Combine(_env.WebRootPath, "uploads", patrolScanId.ToString());
            Directory.CreateDirectory(uploadDir);

            var ext = Path.GetExtension(photo.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }

            scan.PhotoPath = $"/uploads/{patrolScanId}/{fileName}";
            await _db.SaveChangesAsync();
        }

        return RedirectToAction("Active", new { patrolId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> End(int patrolId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var patrol = await _db.Patrols.FirstOrDefaultAsync(w => w.Id == patrolId && w.OfficerId == user.Id);
        if (patrol == null)
            return NotFound();

        await _patrolService.EndPatrolAsync(patrolId, PatrolStatus.Completed);
        TempData["Success"] = "Patrol completed successfully.";
        return RedirectToAction(nameof(History));
    }

    [HttpGet]
    public async Task<IActionResult> History(int page = 1, DateTime? fromDate = null, DateTime? toDate = null, PatrolStatus? status = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var isAdmin = user.Role == UserRole.Administrator || user.Role == UserRole.Auditor;
        var filters = new PatrolFilters
        {
            OfficerId = isAdmin ? null : user.Id,
            FromDate = fromDate,
            ToDate = toDate,
            Status = status,
            Page = page,
            PageSize = 20
        };

        var (items, totalCount) = await _patrolService.GetPatrolHistoryAsync(filters);

        var vm = new PatrolHistoryViewModel
        {
            Patrols = items,
            CurrentPage = page,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / 20),
            FromDate = fromDate,
            ToDate = toDate,
            StatusFilter = status
        };

        return View(vm);
    }
}
