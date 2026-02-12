using MezuroApp.Domain.Entities.Common;

namespace MezuroApp.Domain.Entities;

public class UserAddress:BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string City { get; set; }
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string Country { get; set; } = "Azerbaijan";
    public string AddressType { get; set; } = "shipping";
    public bool IsDefault { get; set; } = false;
    public string? FullName { get; set; }
}