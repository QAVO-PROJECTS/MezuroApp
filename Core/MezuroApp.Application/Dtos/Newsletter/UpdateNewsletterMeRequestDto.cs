namespace MezuroApp.Application.Dtos.Newsletter;

public sealed class UpdateNewsletterMeRequestDto
{
    public bool? IsActive { get; set; }
    public NewsletterPreferencesDto? Preferences { get; set; }
    public string? Frequency { get; set; }
    public string? PreferredLanguage { get; set; }
}