namespace MezuroApp.Application.Dtos.Order.AdminOrder;

public sealed record AdminRefundListFilterDto(
    string? Search,      // order number
    string? Status,      // refunded/partial_refunded/completed etc (biz mapping edəcəyik)
    DateTime? FromUtc,
    DateTime? ToUtc,
    int Page = 1,
    int PageSize = 20
);