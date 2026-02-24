using MezuroApp.Application.Abstracts.Repositories.EmailCampaigns;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.EmailCampaigns;


public sealed class EmailCampaignWriteRepository : WriteRepository<EmailCampaign>, IEmailCampaignWriteRepository
{
    public EmailCampaignWriteRepository(MezuroAppDbContext db) : base(db) { }
}