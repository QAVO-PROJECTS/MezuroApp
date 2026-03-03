namespace MezuroApp.Application.Dtos.Order.AdminOrder;

public sealed record AdminRefundDetailDto(
    Guid PaymentTransactionId,
    Guid OrderId,
    string OrderNumber,
    decimal PaidAmount,
    decimal RefundedAmount,
    string Currency,
    string RefundStatus,
    string PaymentMethod,
    string? TransactionId,
    string? GatewayResponse,
    string? ErrorCode,
    string? ErrorMessage,
    DateTime InitiatedAt,
    DateTime? CompletedAt,
    DateTime LastUpdatedDate
);