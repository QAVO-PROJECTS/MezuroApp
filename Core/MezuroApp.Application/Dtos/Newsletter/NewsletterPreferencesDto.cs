namespace MezuroApp.Application.Dtos.Newsletter;

public sealed class NewsletterPreferencesDto
{
    public bool NewProducts { get; set; } = true;
    public bool Promotions { get; set; } = true;
    public bool WeeklyDigest { get; set; } = false;

    // UI-da var deyə əlavə etdim (sən istəsən DB-də saxla)
    public bool OrderStatusUpdates { get; set; } = true;
}