namespace MezuroApp.Application.Dtos.Order.AdminOrder;

public sealed record AdminOrderItemDto(
    Guid Id,
    string ProductName,
    string? Sku,
    string? Variant,
    int Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal Total,
    string? ImageUrl
);