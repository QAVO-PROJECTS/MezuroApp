namespace MezuroApp.Application.Dtos.Payment;

public sealed record EpointCallbackDto(
    string data,
    string signature
);