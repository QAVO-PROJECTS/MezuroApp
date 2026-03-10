using System.Globalization;
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
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using CloudinaryDotNet.Actions;
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
    private readonly IAuditLogService _audit;
    private readonly IHttpContextAccessor _http;
    
    
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
        ILogger<EmailCampaignService> logger,
        IAuditLogService audit,
        IHttpContextAccessor http)
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
        _audit = audit;
        _http = http;
    }

    public async Task<EmailCampaignDto> CreateAsync(string adminUserId, CreateEmailCampaignDto dto)
    {
        if (!Guid.TryParse(adminUserId, out var adminId))
            throw new GlobalAppException("INVALID_USER_ID");

        var seg = (dto.TargetSegment ?? "").Trim().ToLowerInvariant();
        if (seg is not ("all_active_subscribers" or "verified_users"))
            throw new GlobalAppException("INVALID_TARGET_SEGMENT");
        var scheduledUtc = dto.ScheduleForLater
            ? ParseDdMmYyyyHmToUtcOrThrow(dto.ScheduledAtUtc, "INVALID_SCHEDULED_AT")
            : (DateTime?)null;
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

            CampaignType = "create_promotion", // və ya sənin istədiyin
            TargetSegment = seg,

            Status = dto.ScheduleForLater ? "scheduled" : "draft",
            ScheduledAt = scheduledUtc,

            CreatedById = adminId,
            CreatedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            IsDeleted = false
        };

        await _campaignWrite.AddAsync(campaign);
        await _campaignWrite.CommitAsync();


        await WriteAuditAsync(
            action: "CREATE",
            entityId: campaign.Id,
            oldValues: null,
            newValues: CampaignSnap(campaign)
        );

        return Map(campaign);
  
    }  


    private static DateTime ParseDdMmYyyyHmToUtcOrThrow(string? value, string errorCode, int utcOffsetHours = 4)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new GlobalAppException(errorCode);

        // UI format: dd.MM.yyyy HH:mm
        if (!DateTime.TryParseExact(
                value.Trim(),
                "dd.MM.yyyy HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var localUnspecified))
            throw new GlobalAppException(errorCode);

        // localUnspecified Kind=Unspecified olur.
        // AZ vaxtını (UTC+4) UTC-yə çeviririk:
        var utc = DateTime.SpecifyKind(localUnspecified.AddHours(-utcOffsetHours), DateTimeKind.Utc);

        return utc;
    }
    public async Task<EstimateRecipientsDto> EstimateRecipientsAsync(string targetSegment, CancellationToken ct = default)
    {
        var seg = (targetSegment ?? "").Trim().ToLowerInvariant();

        var q = _subsRead.Query().AsNoTracking().Where(s => !s.IsDeleted);

        q = seg switch
        {
            "verified_users" => q.Where(s => s.User.IsActive == true && s.User.EmailConfirmed),
            _ => q.Where(s => s.User.IsActive == true).Include(x=>x.User) // default all_active_subscribers
        };

        var count = await q.CountAsync(ct);
        return new EstimateRecipientsDto(count);
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
        await EnsureLogsForSegmentAsync(campaign, ct);

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
            TargetSegment = "order_status_updates", // ✅ jsonb üçün düzgün

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
    private async Task EnsureLogsForSegmentAsync(EmailCampaign campaign, CancellationToken ct)
    {
        var existing = await _logRead.GetCountAsync(l =>
            !l.IsDeleted && l.CampaignId == campaign.Id
        );
        if (existing > 0) return;

        if (campaign.CampaignType == "order_status") return;

        var targetSegment = (campaign.TargetSegment ?? "").Trim().ToLowerInvariant();

        if (targetSegment == "all_active_subscribers")
        {
            var subscribers = await _subsRead.Query()
                .AsNoTracking()
                .Where(s => !s.IsDeleted && s.IsActive)
                .ToListAsync(ct);

            foreach (var sub in subscribers)
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
            return;
        }

        if (targetSegment == "verified_users")
        {
            var users = await _userManager.Users
                .Where(u =>
                    u.IsActive == true &&
                    u.EmailConfirmed == true && 
                    !string.IsNullOrWhiteSpace(u.Email))
                .ToListAsync(ct);

            var customers = new List<User>();

            foreach (var user in users)
            {
                if (await _userManager.IsInRoleAsync(user, "Customer"))
                    customers.Add(user);
            }

            
            foreach (var user in customers)
            {
                await _logWrite.AddAsync(new EmailCampaignLog
                {
                    Id = Guid.NewGuid(),
                    CampaignId = campaign.Id,
                    SubscriberId = null,
                    Email = user.Email!,
                    Status = "pending",
                    CreatedDate = DateTime.UtcNow
                });
            }

            await _logWrite.CommitAsync();
            return;
        }

        throw new GlobalAppException("INVALID_TARGET_SEGMENT");
    }
    private async Task EnsureLogsCreatedAsync(EmailCampaign campaign)
    {
        var existing = await _logRead.GetCountAsync(l =>
            !l.IsDeleted && l.CampaignId == campaign.Id
        );

        if (existing > 0) return;

        // segmentə görə subscriber-ları seç
        var subs = await _subsRead.GetAllAsync(s => !s.IsDeleted && s.IsActive, enableTracking: false);

        List<NewsletterSubscriber> eligible = new();

        // TargetSegment-in səndə plain string olduğunu fərz edirik:
        switch ((campaign.TargetSegment ?? "").Trim().ToLowerInvariant())
        {
            case "newsletter_newproduct":
                eligible = subs.Where(s => PrefBool(s.Preferences, "newProducts")).ToList();
                break;

            case "create_promotion":
            case "newsletter_promotion":
                eligible = subs.Where(s => PrefBool(s.Preferences, "promotions")).ToList();
                break;

            case "weekly_digest":
                eligible = subs.Where(s => PrefBool(s.Preferences, "weeklyDigest")).ToList();
                break;

            default:
                // heç nə match olmadısa hamısına göndərmək istəsən:
                // eligible = subs.ToList();
                eligible = new();
                break;
        }

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

    private static bool PrefBool(string? json, string key)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty(key, out var p)) return false;
            return p.ValueKind == JsonValueKind.True;
        }
        catch { return false; }
    }
    public async Task<EmailCampaignDto> ScheduleAsync(string adminUserId, string campaignId, DateTime scheduledAtUtc)
    {
        var campaign = await GetCampaignTracked(campaignId);

        if (campaign.Status is "sending" or "sent")
            throw new GlobalAppException("CAMPAIGN_CANNOT_BE_SCHEDULED");
        var oldSnap = CampaignSnap(campaign);
        await EnsureLogsCreatedAsync(campaign);

        campaign.Status = "scheduled";
        campaign.ScheduledAt = DateTime.SpecifyKind(scheduledAtUtc, DateTimeKind.Utc);
        campaign.LastUpdatedDate = DateTime.UtcNow;

        await _campaignWrite.UpdateAsync(campaign);
        await _campaignWrite.CommitAsync();
        
        await WriteAuditAsync(
            action: "UPDATE",
            entityId: campaign.Id,
            oldValues: oldSnap,
            newValues: CampaignSnap(campaign)
        );

        return Map(campaign);
    }

    public async Task<EmailCampaignDto> SendNowAsync(string adminUserId, string campaignId)
    {
        var campaign = await GetCampaignTracked(campaignId);
        var oldSnap = CampaignSnap(campaign);

        if (campaign.Status is "sending" or "sent")
            throw new GlobalAppException("CAMPAIGN_ALREADY_SENT");

        campaign.Status = "scheduled";
        campaign.ScheduledAt = DateTime.UtcNow;
        campaign.LastUpdatedDate = DateTime.UtcNow;

        await _campaignWrite.UpdateAsync(campaign);
        await _campaignWrite.CommitAsync();
        
        await WriteAuditAsync(
            action: "UPDATE",
            entityId: campaign.Id,
            oldValues: oldSnap,
            newValues: CampaignSnap(campaign)
        );

        return Map(campaign);
    }

    public async Task CancelAsync(string adminUserId, string campaignId)
    {
        var campaign = await GetCampaignTracked(campaignId);
        var oldSnap = CampaignSnap(campaign);

        if (campaign.Status is "sent")
            throw new GlobalAppException("CAMPAIGN_CANNOT_BE_CANCELLED");

        campaign.Status = "cancelled";
        campaign.LastUpdatedDate = DateTime.UtcNow;

        await _campaignWrite.UpdateAsync(campaign);
        await _campaignWrite.CommitAsync();
        await WriteAuditAsync(
            action: "UPDATE", // istəsən "UPDATE" də yaza bilərsən
            entityId: campaign.Id,
            oldValues: oldSnap,
            newValues: CampaignSnap(campaign)
        );
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
    private bool IsAdminRequest()
{
    var user = _http.HttpContext?.User;
    if (user == null) return false;

    // rollara görə (səndə SuperAdmin var)
    if (user.IsInRole("SuperAdmin") || user.IsInRole("Admin") || user.IsInRole("Owner"))
        return true;

    // permission claim varsa da admin say
    return user.Claims.Any(c => c.Type == Permissions.ClaimType);
}

private string GetUserId()
{
    var user = _http.HttpContext?.User;
    return user?.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? user?.FindFirst("sub")?.Value
           ?? "Anonymous";
}

private (string ip, string ua) GetReqInfo()
{
    var ctx = _http.HttpContext;
    var ip = ctx?.Connection.RemoteIpAddress?.ToString() ?? "";
    var ua = ctx?.Request.Headers["User-Agent"].ToString() ?? "";
    return (ip, ua);
}

private static Dictionary<string, object> CampaignSnap(EmailCampaign c) => new()
{
    ["id"] = c.Id.ToString(),
    ["name"] = c.Name,
    ["campaignType"] = c.CampaignType,
    ["targetSegment"] = c.TargetSegment,
    ["status"] = c.Status,
    ["scheduledAt"] = c.ScheduledAt,
    ["sentAt"] = c.SentAt,
    ["totalRecipients"] = c.TotalRecipients,
    ["totalSent"] = c.TotalSent,
    ["totalOpened"] = c.TotalOpened,
    ["totalClicked"] = c.TotalClicked,
    ["totalBounced"] = c.TotalBounced,
    ["totalUnsubscribed"] = c.TotalUnsubscribed,
    ["createdById"] = c.CreatedById.ToString()
};

private async Task WriteAuditAsync(
    string action, // "CREATE" | "UPDATE" | "DELETE"
    Guid? entityId,
    Dictionary<string, object>? oldValues,
    Dictionary<string, object>? newValues)
{
    if (!IsAdminRequest()) return;

    var (ip, ua) = GetReqInfo();

    await _audit.LogAsync(new AuditLog
    {
        UserId = GetUserId(),
         Module = "Campaigns",
        EntityId = entityId,
        ActionType = action,
        OldValuesJson = oldValues ?? new Dictionary<string, object>(),
        NewValuesJson = newValues ?? new Dictionary<string, object>(),
        IpAddress = ip,
        UserAgent = ua,
        CreatedAt = DateTime.UtcNow
    });
}
}