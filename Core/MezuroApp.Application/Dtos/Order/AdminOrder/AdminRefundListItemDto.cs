namespace MezuroApp.Application.Dtos.Order.AdminOrder;


public sealed record AdminRefundListItemDto(
    Guid PaymentTransactionId,
    Guid OrderId,
    string OrderNumber,
    decimal PaidAmount,
    decimal RefundedAmount,
    string Currency,
    string RefundStatus,     // refunded / partial_refunded
    DateTime RefundedAt      // ən real: trx.LastUpdatedDate
);