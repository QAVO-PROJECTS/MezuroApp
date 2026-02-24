namespace MezuroApp.Application.Dtos.Newsletter;

public sealed class NewsletterSubscriberDto
{
    public string Id { get; set; } = default!;
    public string Email { get; set; } = default!;
    public bool IsActive { get; set; }
    public bool IsVerified { get; set; }
    public NewsletterPreferencesDto Preferences { get; set; } = new();
    public string Frequency { get; set; } = default!;
    public string PreferredLanguage { get; set; } = default!;
    public DateTime SubscribedAt { get; set; }
}