namespace MezuroApp.Application.Dtos.Payment;

public sealed record PaymentInitResultDto(
    string OrderId,
    string TransactionId,
    decimal Amount,
    string RedirectUrl
);