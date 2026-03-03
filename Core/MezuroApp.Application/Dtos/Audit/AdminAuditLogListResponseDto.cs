namespace MezuroApp.Application.Dtos.Audit;

public sealed record AdminAuditLogListResponseDto(
    List<AdminAuditLogListItemDto> Items,
    int Page,
    int PageSize,
    long TotalCount
);