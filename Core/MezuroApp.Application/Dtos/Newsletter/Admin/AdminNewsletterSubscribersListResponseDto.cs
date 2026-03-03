using MezuroApp.Domain.HelperEntities;

namespace MezuroApp.Application.Dtos.Newsletter.Admin;

public sealed record AdminNewsletterSubscribersListResponseDto(
    PagedResult<AdminNewsletterSubscriberListItemDto> List,
    AdminNewsletterSubscribersDashboardDto Dashboard
);