using MezuroApp.Application.Dtos.Order;

namespace MezuroApp.Application.Abstracts.Services;

public interface IOrderService
{
    Task<OrderCreatedDto> CreateFromCheckoutAsync(string? userId, CreateOrderCheckoutDto dto);

    Task<List<OrderDto>> GetMyOrdersAsync(string userId);

    Task<List<OrderDto>> GetMyOrdersByStatusAsync(string userId, string status);

    Task<List<OrderDto>> GetMyOrdersByDateAsync(string userId, string dateFilter); // week|month|year



    Task<OrderDetailDto> GetOrderDetailAsync(string userId, string orderId);

    Task SetOrderStatusAsync(string orderId, string newStatus);
}