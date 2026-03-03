namespace MezuroApp.Application.Dtos.Payment;

public sealed record StartEpointPaymentDto(
    string OrderId,
    bool IsInstallment,      // true = taksit, false = standart
    string? FootprintId ,  
    bool SaveCard // NEW
);