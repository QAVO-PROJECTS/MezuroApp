namespace MezuroApp.Application.Dtos.Transaction;

public sealed record AdminRefundResultDto(
    string PaymentTransactionId,
    string RefundStatus,   // refunded / partial_refunded
    decimal RefundedAmount,
    string Currency
);