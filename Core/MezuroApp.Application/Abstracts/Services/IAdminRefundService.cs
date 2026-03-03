using MezuroApp.Application.Dtos.Order.AdminOrder;
using MezuroApp.Domain.HelperEntities;

namespace MezuroApp.Application.Abstracts.Services;

public interface IAdminRefundService
{
    Task<PagedResult<AdminRefundListItemDto>> GetRefundsAsync(AdminRefundListFilterDto filter, CancellationToken ct);
    Task<AdminRefundDetailDto> GetRefundDetailAsync(string paymentTransactionId, CancellationToken ct);
}