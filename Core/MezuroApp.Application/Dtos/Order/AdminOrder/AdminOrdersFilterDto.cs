namespace MezuroApp.Application.Dtos.Order.AdminOrder;

public sealed record AdminOrdersFilterDto(
    string? Search,
    string? Status,
    string? PaymentStatus,
    string? FulfillmentStatus,
    string? FromUtc,
    string? ToUtc,
    decimal? MinAmount,
    decimal? MaxAmount,
    int Page = 1,
    int PageSize = 20
);