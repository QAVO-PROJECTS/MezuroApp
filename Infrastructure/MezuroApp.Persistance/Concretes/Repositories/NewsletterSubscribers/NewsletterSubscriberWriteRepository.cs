using MezuroApp.Application.Abstracts.Repositories.NewsletterSubscribers;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.NewsletterSubscribers;

public sealed class NewsletterSubscriberWriteRepository
    : WriteRepository<NewsletterSubscriber>, INewsletterSubscriberWriteRepository
{
    public NewsletterSubscriberWriteRepository(MezuroAppDbContext dbContext) : base(dbContext) { }
}