using MezuroApp.Application.Dtos.AbandonedCart;
using MezuroApp.Domain.HelperEntities;

namespace MezuroApp.Application.Abstracts.Services;

public interface IAbandonedCartAdminService
{
    Task<AbandonedCartStatsDto> GetStatsAsync(AbandonedCartAdminFilter filter);
    Task<PagedResult<AbandonedCartListItemDto>> GetPagedAsync(AbandonedCartAdminFilter filter, int page, int pageSize);
    Task<AbandonedCartDetailDto> GetDetailAsync(string id);
}