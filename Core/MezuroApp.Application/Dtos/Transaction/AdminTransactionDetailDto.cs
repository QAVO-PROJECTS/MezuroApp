namespace MezuroApp.Application.Dtos.Transaction;

public sealed record AdminTransactionDetailDto(
    string PaymentTransactionId,
    string OrderId,
    string OrderNumber,
    string CustomerName,
    string? CustomerEmail,
    string? CustomerPhone,

    string PaymentMethod,
    string Status,
    decimal Amount,
    decimal RefundedAmount,
    string Currency,

    string InitiatedAt,     // dd.MM.yyyy HH:mm
    string? CompletedAt,    // dd.MM.yyyy HH:mm
    string? TransactionId,  // gateway trx id
    string? GatewayResponse,
    string? ErrorCode,
    string? ErrorMessage
);