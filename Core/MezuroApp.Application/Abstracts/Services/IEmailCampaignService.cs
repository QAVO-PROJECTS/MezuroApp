using MezuroApp.Application.Dtos.EmailCampaigns;
using MezuroApp.Domain.Entities;

namespace MezuroApp.Application.Abstracts.Services;

public interface IEmailCampaignService
{
    Task<EmailCampaignDto> CreateAsync(string adminUserId, CreateEmailCampaignDto dto);
    Task<EmailCampaignDto> ScheduleAsync(string adminUserId, string campaignId, DateTime scheduledAtUtc);
    Task<EmailCampaignDto> SendNowAsync(string adminUserId, string campaignId);
    Task CancelAsync(string adminUserId, string campaignId);

    Task<List<EmailCampaignDto>> GetAllAsync();
    Task<EmailCampaignDto> GetByIdAsync(string campaignId);
    Task SendCampaignInternalAsync(Guid campaignId, CancellationToken ct = default);
    Task CreateAndScheduleNewProductCampaignAsync(Product product); 
    Task CreateAndScheduleOrderStatusCampaignAsync(Order order);

}