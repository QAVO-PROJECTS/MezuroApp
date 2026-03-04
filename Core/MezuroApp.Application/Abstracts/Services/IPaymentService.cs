using MezuroApp.Application.Dtos.Payment;

namespace MezuroApp.Application.Abstracts.Services;

public interface IPaymentService
{
    Task<PaymentInitResultDto> StartEpointAsync(string? userId, StartEpointPaymentDto dto, string? ip, string? userAgent, CancellationToken ct = default);

    Task HandleEpointCallbackAsync(EpointCallbackDto dto, CancellationToken ct = default);


    Task<PaymentStatusDto> GetPaymentStatusAsync(string? userId, string orderId, string? footprintId, CancellationToken ct = default);
    Task AdminReverseEpointAsync(AdminReverseEpointDto dto, CancellationToken ct = default);

    Task<PaymentInitResultDto> PayWithSavedCardAsync(
        string userId,
        PayWithSavedCardDto dto,
        string? ip,
        string? userAgent,
        CancellationToken ct = default);
}