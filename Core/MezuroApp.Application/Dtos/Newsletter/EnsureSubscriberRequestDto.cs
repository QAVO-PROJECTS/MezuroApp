namespace MezuroApp.Application.Dtos.Newsletter;

public sealed class EnsureSubscriberRequestDto
{
    public NewsletterPreferencesDto? Preferences { get; set; }

    // 'daily', 'weekly', 'monthly'
    public string? Frequency { get; set; } = "weekly";

    // 'az', 'en', 'ru', 'tr'
    public string? PreferredLanguage { get; set; } = "az";

    // 'website', 'checkout', 'popup', 'manual', 'register'
    public string? SubscriptionSource { get; set; } = "register";
}