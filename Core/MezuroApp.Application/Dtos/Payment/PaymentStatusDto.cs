namespace MezuroApp.Application.Dtos.Payment;

public sealed record PaymentStatusDto(
    string OrderId,
    string PaymentStatus,      // pending/processing/paid/failed
    string? TransactionId,
    decimal Amount,
    string Currency
);