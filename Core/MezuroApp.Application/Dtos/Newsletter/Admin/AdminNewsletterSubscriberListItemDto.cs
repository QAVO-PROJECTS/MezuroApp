namespace MezuroApp.Application.Dtos.Newsletter.Admin;

public sealed record AdminNewsletterSubscriberListItemDto(
    string Id,
    string Email,
    string Status,           // active/deactivated
    string Language,         // az/en/ru/tr
    string Frequency,        // daily/weekly/monthly
    string SubscriptionDate  // dd.MM.yyyy
);