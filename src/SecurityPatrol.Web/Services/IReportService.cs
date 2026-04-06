using SecurityPatrol.Web.ViewModels;
using SecurityPatrol.Web.Services;

namespace SecurityPatrol.Web.Services;

public interface IReportService
{
    Task<HistoryReportViewModel> GetHistoryReportAsync(PatrolFilters filters);
    Task<RoleReportViewModel> GetRoleReportAsync();
    Task<MissedReportViewModel> GetMissedReportAsync(PatrolFilters filters);
}
