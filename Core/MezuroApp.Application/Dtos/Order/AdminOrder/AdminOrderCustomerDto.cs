namespace MezuroApp.Application.Dtos.Order.AdminOrder;

public sealed record AdminOrderCustomerDto(
    string FullName, 
    string Email, 
    string? Phone);