using MezuroApp.Application.Dtos.Dashboard;

namespace MezuroApp.Application.Abstracts.Services;

public interface IAdminDashboardService
{
    Task<AdminDashboardDto> GetDashboardAsync( CancellationToken ct = default);
}