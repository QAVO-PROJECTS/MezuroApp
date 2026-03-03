namespace MezuroApp.Application.Dtos.AbandonedCart;

public sealed class AbandonedCartDetailDto
{
    public string Id { get; set; } = default!;
    public string? Email { get; set; }
    public string Status { get; set; } = default!;
    public string CreatedAt { get; set; } = default!;
    public string? ExpiryDate { get; set; }
    public decimal TotalAmount { get; set; }

    public string? UserId { get; set; }
    public string? FootprintId { get; set; }
    public string? BasketId { get; set; }

    public bool RecoveryEmailSent { get; set; }
    public string? RecoveryEmailSentAt { get; set; }

    public string? ConvertedToOrderId { get; set; }

    public List<AbandonedCartItemDto> Items { get; set; } = new();
}

public sealed class AbandonedCartItemDto
{
    public string ProductId { get; set; } = default!;
    public string? ProductVariantId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}