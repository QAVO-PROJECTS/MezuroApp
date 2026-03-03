using MezuroApp.Domain.Entities.Common;

namespace MezuroApp.Domain.Entities;

public sealed class AbandonedCart : BaseEntity
{
    // Logged-in user (optional)
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    // Guest/session (səndə session = FootprintId)
    public string? FootprintId { get; set; }

    // Optional: hansı basket snapshot alınmışdı (istəsən saxla)
    public Guid? BasketId { get; set; }
    public Basket? Basket { get; set; }

    // Recovery email üçün
    public string? Email { get; set; }
    public DateTime BasketLastUpdatedSnapshotUtc { get; set; }
    
    // Snapshot cart items (JSONB)
    // məsələn: [{ "productId":"...", "productVariantId":"...", "quantity":2, "unitPrice":316 }, ...]
    public string CartItemsJson { get; set; } = default!;

    public string Status { get; set; }
    public decimal? TotalAmount { get; set; }

    // Recovery email flow
    public bool RecoveryEmailSent { get; set; } = false;
    public DateTime? RecoveryEmailSentAt { get; set; }

    // Order-a çevriləndə
    public Guid? ConvertedToOrderId { get; set; }
    public Order? ConvertedToOrder { get; set; }

    // Expire
    public DateTime? ExpiresAt { get; set; }
    
}