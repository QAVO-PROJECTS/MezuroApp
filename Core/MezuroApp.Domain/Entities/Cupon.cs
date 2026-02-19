using MezuroApp.Domain.Entities.Common;

namespace MezuroApp.Domain.Entities;

public class Cupon:BaseEntity
{
    public string Code { get; set; } = default!;

    // Discount config
    public string DiscountType { get; set; } = default!; // "percentage" | "fixed_amount"
    public decimal DiscountValue { get; set; }

    // Minimum basket limit
    public decimal? MinimumPurchaseAmount { get; set; }

    // Usage limits
    public int? MaxUses { get; set; }
    public int MaxUsesPerUser { get; set; } = 1;
    public int CurrentUses { get; set; }

    // Validity
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid AdminId { get; set; }
    public User? Admin { get; set; }

}