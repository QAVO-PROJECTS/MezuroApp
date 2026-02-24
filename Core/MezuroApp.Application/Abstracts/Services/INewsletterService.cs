using MezuroApp.Application.Dtos.Newsletter;

namespace MezuroApp.Application.Abstracts.Services;

public interface INewsletterService
{
    Task<NewsletterSubscriberDto> SubscribeAsync(SubscribeNewsletterRequestDto dto, CancellationToken ct = default);

    // register zamanı çağırmaq üçün:
    Task SubscribeForUserAsync(Guid userId, string email, string preferredLanguage = "az", CancellationToken ct = default);



    Task UnsubscribeAsync(string email, string? reason = null, CancellationToken ct = default);
    Task<NewsletterSubscriberDto> EnsureForCurrentUserAsync(string userId, EnsureSubscriberRequestDto? dto);
    Task<NewsletterSubscriberDto> GetMeAsync(string userId);
}