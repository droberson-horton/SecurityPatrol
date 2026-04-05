using Microsoft.EntityFrameworkCore;
using QRCoder;
using SecurityPatrol.Web.Data;
using SecurityPatrol.Web.Models;

namespace SecurityPatrol.Web.Services;

public class QrCodeService : IQrCodeService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public QrCodeService(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public string GenerateQrCode(string data)
    {
        using var generator = new QRCodeGenerator();
        // ECCLevel.M (15% recovery) keeps data density lower than Q (25%),
        // producing larger cells at the same image size — easier for cameras to decode.
        var qrCodeData = generator.CreateQrCode(data, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrCodeData);
        // 20px per module produces a ~400px image for a typical short code.
        var pngBytes = qrCode.GetGraphic(20);
        return Convert.ToBase64String(pngBytes);
    }

    public async Task<List<Location>> GetLocationsForPrintAsync(int? buildingId, int? floorId, int? locationId)
    {
        var query = _db.Locations
            .Include(l => l.Building)
            .Include(l => l.Floor)
            .Where(l => l.IsActive)
            .AsQueryable();

        if (locationId.HasValue)
        {
            query = query.Where(l => l.Id == locationId.Value);
        }
        else if (floorId.HasValue)
        {
            query = query.Where(l => l.FloorId == floorId.Value);
        }
        else if (buildingId.HasValue)
        {
            query = query.Where(l => l.BuildingId == buildingId.Value);
        }

        return await query.OrderBy(l => l.Building.Name).ThenBy(l => l.Floor!.FloorNumber).ThenBy(l => l.Name).ToListAsync();
    }
}
