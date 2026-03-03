namespace MezuroApp.Application.Dtos.Payment;

public sealed record AdminReverseEpointDto(
    string OrderId,
    decimal? Amount,   // null => full remaining amount
    string? Note
);