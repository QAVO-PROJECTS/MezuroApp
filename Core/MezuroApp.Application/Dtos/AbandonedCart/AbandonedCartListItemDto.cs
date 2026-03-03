namespace MezuroApp.Application.Dtos.AbandonedCart;

public sealed class AbandonedCartListItemDto
{
    public string Id { get; set; } = default!;
    public string? Email { get; set; }
    public bool IsGuest { get; set; }         // UI-də "Guest" badge üçün
    public string Status { get; set; } = default!;
    public string CreatedAt { get; set; } = default!;
    public string? ExpiryDate { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemsCount { get; set; }
    public bool RecoveryEmailSent { get; set; }
}