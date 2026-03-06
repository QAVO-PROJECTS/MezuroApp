using MezuroApp.Application.Dtos.Dashboard;

namespace MezuroApp.Application.Abstracts.Services;

public interface IAdminDashboardService
{
    Task<AdminDashboardDto> GetDashboardAsync(AdminDashboardFilterDto filter, CancellationToken ct = default);
}