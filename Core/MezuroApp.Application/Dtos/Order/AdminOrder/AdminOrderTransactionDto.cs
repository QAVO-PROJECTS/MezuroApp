namespace MezuroApp.Application.Dtos.Order.AdminOrder;

public sealed record AdminOrderTransactionDto(
    Guid Id,
    string Status,
    decimal Amount,
    decimal RefundedAmount,
    string Currency,
    string PaymentMethod,
    string? TransactionId,
    DateTime InitiatedAt
);