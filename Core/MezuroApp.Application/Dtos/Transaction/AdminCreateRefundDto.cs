namespace MezuroApp.Application.Dtos.Transaction;

public sealed record AdminCreateRefundDto(
    string PaymentTransactionId, // UI modal-dan gələcək
    string? Reason               // optional
);