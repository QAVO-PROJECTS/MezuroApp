namespace MezuroApp.Application.Dtos.Newsletter.Admin;

public sealed record AdminNewsletterSubscribersDashboardDto(
    int TotalSubscribers,
    int ActiveSubscribers,
    int VerifiedSubscribers,
    int UnsubscribedSubscribers
);