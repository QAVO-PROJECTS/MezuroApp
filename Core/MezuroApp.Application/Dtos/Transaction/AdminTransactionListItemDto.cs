namespace MezuroApp.Application.Dtos.Transaction;

public sealed record AdminTransactionListItemDto(
    string PaymentTransactionId,
    string OrderId,
    string OrderNumber,
    string CustomerName,
    string PaymentMethod,
    string Status,
    decimal Amount,
    decimal RefundedAmount,
    string Currency,
    string Date // dd.MM.yyyy
);