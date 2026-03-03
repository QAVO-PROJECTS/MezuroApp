namespace MezuroApp.Application.Dtos.Order.AdminOrder;

public sealed record AdminOrderDetailDto(
    Guid Id,
    string OrderNumber,
    DateTime CreatedDate,
    string Currency,
    decimal SubTotal,
    decimal Discount,
    decimal Shipping,
    decimal Tax,
    decimal Total,
    string PaymentMethod,
    string PaymentStatus,
    string FulfillmentStatus,
    string Status,
    AdminOrderCustomerDto Customer,
    List<AdminOrderItemDto> Items,
    List<AdminOrderTransactionDto> Transactions,
    List<AdminRefundListItemDto> Refunds
);
