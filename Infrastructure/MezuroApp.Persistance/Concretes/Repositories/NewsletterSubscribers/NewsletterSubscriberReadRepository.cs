using MezuroApp.Application.Abstracts.Repositories.NewsletterSubscribers;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.NewsletterSubscribers;

public sealed class NewsletterSubscriberReadRepository
    : ReadRepository<NewsletterSubscriber>, INewsletterSubscriberReadRepository
{
    public NewsletterSubscriberReadRepository(MezuroAppDbContext dbContext) : base(dbContext) { }
}