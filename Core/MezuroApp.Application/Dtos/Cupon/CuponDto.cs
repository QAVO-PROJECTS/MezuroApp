using MezuroApp.Application.Dtos.Auth;

namespace MezuroApp.Application.Dtos.Cupon;

public class CuponDto
{
    public string Id { get; set; }
    public string Code { get; set; }

    // Discount config
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }

    // Minimum basket limit
    public decimal? MinimumPurchaseAmount { get; set; }

    // Usage limits
    public int? MaxUses { get; set; }
    public int MaxUsesPerUser { get; set; }
    public int CurrentUses { get; set; }

    // Validity
    public string? ValidFrom { get; set; }
    public string? ValidUntil { get; set; }
    public string CreatedDate { get; set; }

    public bool IsActive { get; set; }

    public UserDto? Admin{ get; set; }
}