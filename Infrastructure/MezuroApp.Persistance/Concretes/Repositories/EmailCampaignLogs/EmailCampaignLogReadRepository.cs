using MezuroApp.Application.Abstracts.Repositories.EmailCampaignLogs;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.EmailCampaignLogs;


public sealed class EmailCampaignLogReadRepository : ReadRepository<EmailCampaignLog>, IEmailCampaignLogReadRepository
{
    public EmailCampaignLogReadRepository(MezuroAppDbContext db) : base(db) { }
}