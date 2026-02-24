namespace MezuroApp.Application.Dtos.Newsletter;

public sealed class SubscribeNewsletterRequestDto
{
    public string Email { get; set; } = default!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    // 'website', 'checkout', 'popup', 'manual'
    public string? SubscriptionSource { get; set; }

    public string? PreferredLanguage { get; set; } = "az";
    public string? Frequency { get; set; } = "weekly";

    public NewsletterPreferencesDto? Preferences { get; set; }
}