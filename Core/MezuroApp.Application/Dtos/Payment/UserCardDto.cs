namespace MezuroApp.Application.Dtos.Payment;

public sealed record UserCardDto(
    Guid Id,
    string? CardName,
    string? CardMask,
    string? CardExpiry,
    bool IsDefault
);

public sealed record SetDefaultCardDto(Guid CardId);