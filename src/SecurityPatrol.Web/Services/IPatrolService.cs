using SecurityPatrol.Web.Models;
using SecurityPatrol.Web.ViewModels;

namespace SecurityPatrol.Web.Services;

public class PatrolFilters
{
    public string? OfficerId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public TimeSpan? TimeFrom { get; set; }
    public TimeSpan? TimeTo { get; set; }
    public PatrolStatus? Status { get; set; }
    public int? BuildingId { get; set; }
    public int? FloorId { get; set; }
    public int? LocationId { get; set; }
    public bool MissedOnly { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public interface IPatrolService
{
    Task<List<MissedLocationItem>> GetMissedLocationsAsync(PatrolFilters filters);
    Task<PatrolActiveViewModel> GetPatrolProgressAsync(int patrolId);
    Task<Location?> ValidateQrCodeAsync(string qrCode);
    Task<(List<PatrolHistoryItem> items, int totalCount)> GetPatrolHistoryAsync(PatrolFilters filters);
    Task<Patrol?> GetActivePatrolAsync(string officerId);
    Task<Patrol> StartPatrolAsync(string officerId, int patrolRouteId, int? scheduledPatrolId);
    Task<PatrolScan> RecordScanAsync(int patrolId, int locationId);
    Task EndPatrolAsync(int patrolId, PatrolStatus status);
}
