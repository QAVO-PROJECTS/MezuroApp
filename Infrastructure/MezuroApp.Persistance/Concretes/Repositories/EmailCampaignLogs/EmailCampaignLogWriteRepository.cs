using MezuroApp.Application.Abstracts.Repositories.EmailCampaignLogs;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.EmailCampaignLogs;

public sealed class EmailCampaignLogWriteRepository : WriteRepository<EmailCampaignLog>, IEmailCampaignLogWriteRepository
{
    public EmailCampaignLogWriteRepository(MezuroAppDbContext db) : base(db) { }
}