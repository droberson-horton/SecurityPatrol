using SecurityPatrol.Web.Models;

namespace SecurityPatrol.Web.Services;

public interface IQrCodeService
{
    string GenerateQrCode(string data);
    Task<List<Location>> GetLocationsForPrintAsync(int? buildingId, int? floorId, int? locationId);
}
