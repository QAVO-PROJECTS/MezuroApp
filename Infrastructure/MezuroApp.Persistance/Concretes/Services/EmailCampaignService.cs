using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using MezuroApp.Application.Abstracts.Repositories.EmailCampaigns;
using MezuroApp.Application.Abstracts.Repositories.EmailCampaignLogs;
using MezuroApp.Application.Abstracts.Repositories.NewsletterSubscribers;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.EmailCampaigns;
using MezuroApp.Application.Dtos.Newsletter;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;
using MezuroApp.Domain.HelperEntities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace MezuroApp.Persistance.Concretes.Services;

public sealed class EmailCampaignService : IEmailCampaignService
{
    private readonly IEmailCampaignReadRepository _campaignRead;
    private readonly IEmailCampaignWriteRepository _campaignWrite;

    private readonly IEmailCampaignLogReadRepository _logRead;
    private readonly IEmailCampaignLogWriteRepository _logWrite;

    private readonly INewsletterSubscriberReadRepository _subsRead;

    private readonly UserManager<User> _userManager;
    private readonly IMailService _mail;
    private readonly IConfiguration _cfg;
    private readonly ILogger<EmailCampaignService> _logger; 
    
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public EmailCampaignService(
        IEmailCampaignReadRepository campaignRead,
        IEmailCampaignWriteRepository campaignWrite,
        IEmailCampaignLogReadRepository logRead,
        IEmailCampaignLogWriteRepository logWrite,
        INewsletterSubscriberReadRepository subsRead,
        UserManager<User> userManager,
        IMailService mail,
        IConfiguration cfg,
        ILogger<EmailCampaignService> logger)
    {
        _campaignRead = campaignRead;
        _campaignWrite = campaignWrite;
        _logRead = logRead;
        _logWrite = logWrite;
        _subsRead = subsRead;
        _userManager = userManager;
        _mail = mail;
        _cfg = cfg;
        _logger = logger;
    }

    public async Task<EmailCampaignDto> CreateAsync(string adminUserId, CreateEmailCampaignDto dto)
    {
        if (!Guid.TryParse(adminUserId, out var adminId))
            throw new GlobalAppException("INVALID_USER_ID");

        var campaign = new EmailCampaign
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,

            SubjectAz = dto.SubjectAz,
            SubjectRu = dto.SubjectRu,
            SubjectEn = dto.SubjectEn,
            SubjectTr = dto.SubjectTr,

            ContentAz = dto.ContentAz,
            ContentRu = dto.ContentRu,
            ContentEn = dto.ContentEn,
            ContentTr = dto.ContentTr,

            CampaignType = dto.CampaignType,
            TargetSegment = dto.TargetSegment,

            Status = "draft",
            CreatedById = adminId,

            CreatedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            IsDeleted = false
        };

        await _campaignWrite.AddAsync(campaign);
        await _campaignWrite.CommitAsync();

        return Map(campaign);
    }
public async Task SendCampaignInternalAsync(Guid campaignId, CancellationToken ct = default)
    
    {
        // 1) Campaign (tracking ON)
        var campaign = await _campaignRead.GetAsync(
            c => !c.IsDeleted && c.Id == campaignId,
            enableTracking: true
        );

        if (campaign == null)
            throw new GlobalAppException("CAMPAIGN_NOT_FOUND");

        // scheduled -> sending keçməyibsə də göndərməyə icazə verək
        if (campaign.Status != "sending" && campaign.Status != "scheduled")
            return;

        var batchSize = _cfg.GetValue<int>("EmailCampaign:BatchSize", 50);

        // (İstəyə bağlı) TotalRecipients hesabla: pending log sayına görə
        // bunu yalnız 1 dəfə set edək (0-dırsa)
        if (campaign.TotalRecipients == 0)
        {
            var totalPending = await _logRead.GetCountAsync(l =>
                !l.IsDeleted
                && l.CampaignId == campaign.Id
                && l.Status == "pending"
            );

            campaign.TotalRecipients = totalPending;
            campaign.LastUpdatedDate = DateTime.UtcNow;
            await _campaignWrite.UpdateAsync(campaign);
            await _campaignWrite.CommitAsync();
        }

        int sentNow = 0;
        int failedNow = 0;

        while (!ct.IsCancellationRequested)
        {
            // 2) Pending log-ları götür (tracking ON -> status update edəcəyik)
            // Repo-da GetPagedAsync yoxdursa: GetAllAsync + Take(batchSize) et.
            var pending = await _logRead.GetAllAsync(
                l => !l.IsDeleted
                     && l.CampaignId == campaign.Id
                     && l.Status == "pending",
                enableTracking: true
            );

            var batch = pending
                .OrderBy(x => x.CreatedDate)
                .Take(batchSize)
                .ToList();

            if (batch.Count == 0)
                break;

            foreach (var log in batch)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    // 3) Subscriber check (əgər log subscriber bağlıdırsa)
                    NewsletterSubscriber? sub = null;

                    if (log.SubscriberId != null)
                    {
                        sub = await _subsRead.GetAsync(
                            s => !s.IsDeleted && s.Id == log.SubscriberId.Value,
                            enableTracking: false
                        );
                    }

                    // subscriber varsa və inactive-dirsə -> skip/fail
                    if (sub != null && sub.IsActive == false)
                        throw new Exception("SUBSCRIBER_INACTIVE");

                    // 4) Language seç (log-da saxlamırsansa subscriber-dən oxu, yoxdursa campaign default az)
                    var lang = sub?.PreferredLanguage;
                    if (string.IsNullOrWhiteSpace(lang)) lang = "az";

                    // 5) Subject / Content seç
                    var subject = PickSubject(campaign, lang);
                    var body = PickContent(campaign, lang);

                    // (İstəsən) Unsubscribe link və tracking pixel burda inject edilə bilər.

                    // 6) Mail göndər
                    await _mail.SendEmailAsync(new MailRequest
                    {
                        ToEmail = log.Email,
                        Subject = subject,
                        Body = body
                    });

                    // 7) Log update -> sent
                    log.Status = "sent";
                    log.SentAt = DateTime.UtcNow;
                    log.ErrorMessage = null;

                    sentNow++;
                    campaign.TotalSent += 1;
                }
                catch (Exception ex)
                {
                    // Log update -> failed
                    log.Status = "failed";
                    log.ErrorMessage = ex.Message?.Length > 500 ? ex.Message[..500] : ex.Message;
                    log.SentAt = DateTime.UtcNow;

                    failedNow++;
                    campaign.TotalBounced += 1; // bura bounced yox, failed saymaq istəyirsənsə ayrıca TotalFailed saxla
                }
                finally
                {
                    log.LastUpdatedDate = DateTime.UtcNow;
                    await _logWrite.UpdateAsync(log);
                }
            }

            // 8) batch commit
            campaign.LastUpdatedDate = DateTime.UtcNow;
            await _campaignWrite.UpdateAsync(campaign);

            await _logWrite.CommitAsync();
            await _campaignWrite.CommitAsync();

            _logger.LogInformation(
                "Campaign {CampaignId} batch done. batchSent={Sent} batchFailed={Failed}",
                campaign.Id, sentNow, failedNow
            );
        }

        // 9) Artıq pending yoxdursa -> sent et
        var left = await _logRead.GetCountAsync(l =>
            !l.IsDeleted && l.CampaignId == campaign.Id && l.Status == "pending"
        );

        if (left == 0)
        {
            campaign.Status = "sent";
            campaign.SentAt = DateTime.UtcNow;
            campaign.LastUpdatedDate = DateTime.UtcNow;

            await _campaignWrite.UpdateAsync(campaign);
            await _campaignWrite.CommitAsync();
        }
    }
    public async Task CreateAndScheduleOrderStatusCampaignAsync(Order order)
    {
        // order.Email boş olmamalıdır
        if (string.IsNullOrWhiteSpace(order.Email)) return;

        var segmentJson = JsonSerializer.Serialize(new { type = "order_status_updates" });

        var campaign = new EmailCampaign
        {
            Id = Guid.NewGuid(),
            Name = $"Order Status - {order.OrderNumber}",

            SubjectAz = "Sifariş statusunuz yeniləndi",
            SubjectEn = "Your order status has been updated",
            SubjectRu = "Статус вашего заказа обновлен",
            SubjectTr = "Sipariş durumunuz güncellendi",

            // HTML template-i SendCampaignInternalAsync-də quracaqsan
            ContentAz = $"Sifariş: {order.OrderNumber} | Yeni status: {order.Status}",
            ContentEn = $"Order: {order.OrderNumber} | New status: {order.Status}",
            ContentRu = $"Заказ: {order.OrderNumber} | Новый статус: {order.Status}",
            ContentTr = $"Sipariş: {order.OrderNumber} | Yeni durum: {order.Status}",

            CampaignType = "order_status",
            TargetSegment = segmentJson, // ✅ jsonb üçün düzgün

            Status = "scheduled",
            ScheduledAt = DateTime.UtcNow,

            CreatedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            IsDeleted = false
        };

        await _campaignWrite.AddAsync(campaign);
        await _campaignWrite.CommitAsync();

        // ✅ bu campaign spesifik 1 email-ə gedəcək
        await _logWrite.AddAsync(new EmailCampaignLog
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            SubscriberId = null,         // əgər subscriber deyilirsə null ola bilər
            Email = order.Email.Trim(),
            Status = "pending",
            CreatedDate = DateTime.UtcNow
        });

        await _logWrite.CommitAsync();
    }
    public async Task CreateAndScheduleNewProductCampaignAsync(Product product)
    {
        var campaign = new EmailCampaign
        {
            Id = Guid.NewGuid(),
            Name = $"New Product - {product.NameAz}",

            SubjectAz = "Yeni məhsul əlavə edildi!",
            SubjectEn = "New product just arrived!",
            SubjectRu = "Новый продукт добавлен!",
            SubjectTr = "Yeni ürün eklendi!",

            ContentAz = $"Yeni məhsulumuzla tanış olun: {product.NameAz}",
            ContentEn = $"Check out our new product: {product.NameEn}",
            ContentRu = $"Ознакомьтесь с нашим новым продуктом: {product.NameRu}",
            ContentTr = $"Yeni ürünümüzü keşfedin: {product.NameTr}",

            CampaignType = "product",
            TargetSegment = "newsletter_newproduct",

            Status = "scheduled",
            ScheduledAt = DateTime.UtcNow,

            CreatedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow
        };

        await _campaignWrite.AddAsync(campaign);
        await _campaignWrite.CommitAsync();

        // 🔥 subscriber filter
        var subscribers = await _subsRead.GetAllAsync(
            s => !s.IsDeleted && s.IsActive,
            enableTracking: false
        );

        var eligible = subscribers
            .Where(s =>
            {
                try
                {
                    var pref = JsonSerializer.Deserialize<NewsletterPreferencesDto>(s.Preferences);
                    return pref?.NewProducts == true;
                }
                catch
                {
                    return false;
                }
            })
            .ToList();

        foreach (var sub in eligible)
        {
            await _logWrite.AddAsync(new EmailCampaignLog
            {
                Id = Guid.NewGuid(),
                CampaignId = campaign.Id,
                SubscriberId = sub.Id,
                Email = sub.Email,
                Status = "pending",
                CreatedDate = DateTime.UtcNow
            });
        }

        await _logWrite.CommitAsync();
    }
    private static string PickSubject(EmailCampaign c, string lang)
    {
        lang = lang.Trim().ToLowerInvariant();
        return lang switch
        {
            "en" => c.SubjectEn,
            "ru" => c.SubjectRu,
            "tr" => c.SubjectTr,
            _ => c.SubjectAz
        };
    }

    private static string PickContent(EmailCampaign c, string lang)
    {
        lang = lang.Trim().ToLowerInvariant();
        return lang switch
        {
            "en" => c.ContentEn,
            "ru" => c.ContentRu,
            "tr" => c.ContentTr,
            _ => c.ContentAz
        };
    }
    public async Task<EmailCampaignDto> ScheduleAsync(string adminUserId, string campaignId, DateTime scheduledAtUtc)
    {
        var campaign = await GetCampaignTracked(campaignId);

        if (campaign.Status is "sending" or "sent")
            throw new GlobalAppException("CAMPAIGN_CANNOT_BE_SCHEDULED");

        campaign.Status = "scheduled";
        campaign.ScheduledAt = DateTime.SpecifyKind(scheduledAtUtc, DateTimeKind.Utc);
        campaign.LastUpdatedDate = DateTime.UtcNow;

        await _campaignWrite.UpdateAsync(campaign);
        await _campaignWrite.CommitAsync();

        return Map(campaign);
    }

    public async Task<EmailCampaignDto> SendNowAsync(string adminUserId, string campaignId)
    {
        var campaign = await GetCampaignTracked(campaignId);

        if (campaign.Status is "sending" or "sent")
            throw new GlobalAppException("CAMPAIGN_ALREADY_SENT");

        campaign.Status = "scheduled";
        campaign.ScheduledAt = DateTime.UtcNow;
        campaign.LastUpdatedDate = DateTime.UtcNow;

        await _campaignWrite.UpdateAsync(campaign);
        await _campaignWrite.CommitAsync();

        return Map(campaign);
    }

    public async Task CancelAsync(string adminUserId, string campaignId)
    {
        var campaign = await GetCampaignTracked(campaignId);

        if (campaign.Status is "sent")
            throw new GlobalAppException("CAMPAIGN_CANNOT_BE_CANCELLED");

        campaign.Status = "cancelled";
        campaign.LastUpdatedDate = DateTime.UtcNow;

        await _campaignWrite.UpdateAsync(campaign);
        await _campaignWrite.CommitAsync();
    }

    public async Task<List<EmailCampaignDto>> GetAllAsync()
    {
        var list = await _campaignRead.GetAllAsync(x => !x.IsDeleted,
            orderBy: q => q.OrderByDescending(x => x.CreatedDate));

        return list.Select(Map).ToList();
    }

    public async Task<EmailCampaignDto> GetByIdAsync(string campaignId)
    {
        var campaign = await _campaignRead.GetByIdAsync(campaignId);
        return Map(campaign);
    }

    // =========================
    // BackgroundService burdan istifadə edəcək:
    // =========================


    private static (string subject, string content) PickLocalizedContent(EmailCampaign c, string? lang)
    {
        lang = (lang ?? "az").Trim().ToLowerInvariant();

        return lang switch
        {
            "en" => (c.SubjectEn, c.ContentEn),
            "ru" => (c.SubjectRu, c.ContentRu),
            "tr" => (c.SubjectTr, c.ContentTr),
            _ => (c.SubjectAz, c.ContentAz)
        };
    }

    private async Task<EmailCampaign> GetCampaignTracked(string id)
        => await _campaignRead.GetAsync(x => x.Id == Guid.Parse(id) && !x.IsDeleted, enableTracking: true)
           ?? throw new GlobalAppException("CAMPAIGN_NOT_FOUND");

    private static EmailCampaignDto Map(EmailCampaign c) => new()
    {
        Id = c.Id.ToString(),
        Name = c.Name,
        CampaignType = c.CampaignType,
        Status = c.Status,
        ScheduledAt = c.ScheduledAt,
        SentAt = c.SentAt,
        TotalRecipients = c.TotalRecipients,
        TotalSent = c.TotalSent,
        TotalOpened = c.TotalOpened,
        TotalClicked = c.TotalClicked,
        TotalBounced = c.TotalBounced,
        TotalUnsubscribed = c.TotalUnsubscribed
    };
}