namespace MezuroApp.Application.Dtos.Payment;

public sealed record PayWithSavedCardDto(
    string OrderId,
    Guid UserCardId ,
 bool FallbackTo3DS ,
 bool IsInstallment
);