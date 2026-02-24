namespace MezuroApp.Persistance.Concretes.Repositories.EmailCampaigns;
using MezuroApp.Application.Abstracts.Repositories.EmailCampaigns;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

public sealed class EmailCampaignReadRepository : ReadRepository<EmailCampaign>, IEmailCampaignReadRepository
{
    public EmailCampaignReadRepository(MezuroAppDbContext db) : base(db) { }
}