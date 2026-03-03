namespace MezuroApp.Application.Dtos.Payment;

public sealed record CardSaveInitResultDto(
    string CardUid,     // epoint-in init cavabında gəlir
    string RedirectUrl
);