using MezuroApp.Application.Dtos.Order.AdminOrder;
using MezuroApp.Domain.HelperEntities;

namespace MezuroApp.Application.Abstracts.Services;

public interface IAdminOrderService
{
    Task<PagedResult<AdminOrderListItemDto>> GetOrdersAsync(AdminOrdersFilterDto filter, CancellationToken ct);
    Task<AdminOrderDetailDto> GetOrderDetailAsync(string orderId, CancellationToken ct);
    Task SetOrderStatusAsync(string orderId, string newStatus, CancellationToken ct);
    Task CancelOrderAsync(string orderId, string? adminNote, CancellationToken ct);
    Task ResendConfirmationAsync(string orderId, CancellationToken ct);
}
