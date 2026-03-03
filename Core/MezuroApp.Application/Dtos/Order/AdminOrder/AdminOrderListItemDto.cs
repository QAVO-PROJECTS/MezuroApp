namespace MezuroApp.Application.Dtos.Order.AdminOrder;

public sealed record AdminOrderListItemDto(
    Guid Id,
    string OrderNumber,
    string CustomerName,
    decimal Total,
    string Currency,
    string PaymentStatus,
    string FulfillmentStatus,
    string Status,
    string CreatedDate
);