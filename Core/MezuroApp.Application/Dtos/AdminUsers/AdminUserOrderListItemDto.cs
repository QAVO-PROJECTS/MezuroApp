namespace MezuroApp.Application.Dtos.AdminUsers;

public sealed class AdminUserOrderListItemDto
{
    public string Id { get; set; } = default!;
    public string OrderNumber { get; set; } = default!;
    public string Status { get; set; } = default!;
    public decimal Total { get; set; }
    public string OrderDate { get; set; } = default!;
}