using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using MezuroApp.Application.Abstracts.Repositories.NewsletterSubscribers;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Newsletter;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;

namespace MezuroApp.Persistance.Concretes.Services;

public sealed class NewsletterService : INewsletterService
{
    private readonly INewsletterSubscriberReadRepository _read;
    private readonly INewsletterSubscriberWriteRepository _write;
    private readonly UserManager<User> _userManager;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public NewsletterService(
        INewsletterSubscriberReadRepository read,
        INewsletterSubscriberWriteRepository write,
        UserManager<User> userManager)
    {
        _read = read;
        _write = write;
        _userManager = userManager;
    }

    // ================
    // 1) Subscribe (public)
    // ================
    public async Task<NewsletterSubscriberDto> SubscribeAsync(SubscribeNewsletterRequestDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            throw new GlobalAppException("EMAIL_REQUIRED");

        var email = dto.Email.Trim().ToLowerInvariant();
        if (!email.Contains("@"))
            throw new GlobalAppException("INVALID_EMAIL");

        var sub = await _read.GetAsync(x => x.Email.ToLower() == email && !x.IsDeleted, enableTracking: true);

        var pref = dto.Preferences ?? DefaultPreferences();
        var freq = NormalizeFrequency(dto.Frequency) ?? "weekly";
        var lang = NormalizeLang(dto.PreferredLanguage) ?? "az";

        if (sub == null)
        {
            sub = new NewsletterSubscriber
            {
                Id = Guid.NewGuid(),
                Email = email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,

                IsActive = true,
                IsVerified = false, // public subscribe üçün
                VerifiedAt = null,

                SubscriptionSource = string.IsNullOrWhiteSpace(dto.SubscriptionSource) ? "website" : dto.SubscriptionSource,

                Preferences = JsonSerializer.Serialize(pref, JsonOpts),
                Frequency = freq,
                PreferredLanguage = lang,

                SubscribedAt = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow,
                LastUpdatedDate = DateTime.UtcNow,
                IsDeleted = false
            };

            await _write.AddAsync(sub);
            await _write.CommitAsync();
            return Map(sub);
        }

        // re-subscribe
        sub.IsActive = true;
        sub.UnsubscribedAt = null;
        sub.UnsubscribeReason = null;

        if (!string.IsNullOrWhiteSpace(dto.FirstName)) sub.FirstName = dto.FirstName;
        if (!string.IsNullOrWhiteSpace(dto.LastName)) sub.LastName = dto.LastName;

        sub.Preferences = JsonSerializer.Serialize(pref, JsonOpts);
        sub.Frequency = freq;
        sub.PreferredLanguage = lang;

        sub.LastUpdatedDate = DateTime.UtcNow;

        await _write.UpdateAsync(sub);
        await _write.CommitAsync();

        return Map(sub);
    }

    // ================
    // 2) SubscribeForUser (register zamanı çağırırsan)
    // ================
    public async Task SubscribeForUserAsync(Guid userId, string email, string preferredLanguage = "az", CancellationToken ct = default)
    {
        if (userId == Guid.Empty) throw new GlobalAppException("INVALID_USER_ID");
        if (string.IsNullOrWhiteSpace(email)) throw new GlobalAppException("EMAIL_REQUIRED");

        var normalized = email.Trim().ToLowerInvariant();

        var sub = await _read.GetAsync(
            x => !x.IsDeleted && x.Email == email,
            enableTracking: true
        );

        if (sub == null)
        {
            sub = new NewsletterSubscriber
            {
                Id = Guid.NewGuid(),
                Email = normalized,
                UserId = userId,

                IsActive = true,
                SubscriptionSource = "register",

                Preferences = JsonSerializer.Serialize(DefaultPreferences(), JsonOpts),
                Frequency = "weekly",
                PreferredLanguage = NormalizeLang(preferredLanguage) ?? "az",

                // register mail confirmeddən sonra ensure endpoint bunu update edəcək
                IsVerified = false,

                SubscribedAt = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow,
                LastUpdatedDate = DateTime.UtcNow,
                IsDeleted = false
            };

            await _write.AddAsync(sub);
            await _write.CommitAsync();
            return;
        }

        sub.UserId ??= userId;
        sub.IsActive = true;
        sub.SubscriptionSource ??= "register";
        sub.PreferredLanguage = NormalizeLang(preferredLanguage) ?? sub.PreferredLanguage;

        sub.LastUpdatedDate = DateTime.UtcNow;

        await _write.UpdateAsync(sub);
        await _write.CommitAsync();
    }

    // ================
    // 3) Unsubscribe (public)
    // ================
    public async Task UnsubscribeAsync(string email, string? reason = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new GlobalAppException("EMAIL_REQUIRED");

        var normalized = email.Trim().ToLowerInvariant();

        var sub = await _read.GetAsync(x => x.Email.ToLower() == normalized && !x.IsDeleted, enableTracking: true)
                  ?? throw new GlobalAppException("SUBSCRIBER_NOT_FOUND");

        sub.IsActive = false;
        sub.UnsubscribedAt = DateTime.UtcNow;
        sub.UnsubscribeReason = reason;
        sub.LastUpdatedDate = DateTime.UtcNow;

        await _write.UpdateAsync(sub);
        await _write.CommitAsync();
    }

    // ================
    // 4) EnsureForCurrentUser (logged-in user üçün)
    // ================
public async Task<NewsletterSubscriberDto> EnsureForCurrentUserAsync(string userId, EnsureSubscriberRequestDto? dto)
{
    if (!Guid.TryParse(userId, out var uid))
        throw new GlobalAppException("INVALID_USER_ID");

    var user = await _userManager.FindByIdAsync(userId);
    if (user == null || string.IsNullOrWhiteSpace(user.Email))
        throw new GlobalAppException("USER_NOT_FOUND");

    var email = user.Email.Trim().ToLowerInvariant();

    // ✅ Eyni email-ə aid hamısını çək (duplicate varsa merge edəcəyik)
    var subs = await _read.GetAllAsync(
        x => !x.IsDeleted && x.Email.ToLower() == email,
        enableTracking: true
    );

    var pref = dto?.Preferences ?? DefaultPreferences();
    var freq = NormalizeFrequency(dto?.Frequency) ?? "weekly";
    var lang = NormalizeLang(dto?.PreferredLanguage) ?? "az";

    // ✅ subscriber yoxdursa create
    if (subs == null || subs.Count == 0)
    {
        var created = new NewsletterSubscriber
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = user.FirstName,
            LastName = user.LastName,

            UserId = uid,
            IsActive = true,

            IsVerified = user.EmailConfirmed,
            VerifiedAt = user.EmailConfirmed ? DateTime.UtcNow : null,

            SubscriptionSource = "profile",

            Preferences = JsonSerializer.Serialize(pref, JsonOpts),
            Frequency = freq,
            PreferredLanguage = lang,

            SubscribedAt = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            IsDeleted = false
        };

        await _write.AddAsync(created);
        await _write.CommitAsync();
        return Map(created);
    }

    // ✅ PRIMARY seç: UserId olan varsa onu götür, yoxdursa ən yenisini götür
    var primary =
        subs.FirstOrDefault(x => x.UserId == uid)
        ?? subs.FirstOrDefault(x => x.UserId != null)
        ?? subs.OrderByDescending(x => x.CreatedDate).First();

    // ✅ primary-ni user-ə bağla + update et
    primary.IsActive = true;
    primary.UserId = uid;
    primary.FirstName ??= user.FirstName;
    primary.LastName ??= user.LastName;

    // dto gəlməyibsə guest preferences saxlanılsın istəyirsənsə bu 3 sətri ŞƏRTLİ et:
    primary.Preferences = JsonSerializer.Serialize(pref, JsonOpts);
    primary.Frequency = freq;
    primary.PreferredLanguage = lang;

    primary.IsVerified = user.EmailConfirmed;
    if (primary.IsVerified && primary.VerifiedAt == null)
        primary.VerifiedAt = DateTime.UtcNow;

    primary.LastUpdatedDate = DateTime.UtcNow;

    // ✅ qalan duplicate-ləri soft delete et
    foreach (var other in subs.Where(x => x.Id != primary.Id))
    {
        other.IsDeleted = true;
        other.DeletedDate = DateTime.UtcNow;
        other.LastUpdatedDate = DateTime.UtcNow;
        await _write.UpdateAsync(other);
    }

    await _write.UpdateAsync(primary);
    await _write.CommitAsync();

    return Map(primary);
}

    // ================
    // 5) GetMe (logged-in user)
    // ================
    public async Task<NewsletterSubscriberDto> GetMeAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
            throw new GlobalAppException("USER_NOT_FOUND");

        var email = user.Email.Trim().ToLowerInvariant();

        var sub = await _read.GetAsync(x => x.Email.ToLower() == email && !x.IsDeleted);
        if (sub == null)
            throw new GlobalAppException("SUBSCRIBER_NOT_FOUND");

        return Map(sub);
    }

    // =======================
    // helpers
    // =======================
    private static NewsletterPreferencesDto DefaultPreferences()
        => new() { NewProducts = true, Promotions = true, WeeklyDigest = false, OrderStatusUpdates = true };

    private static string? NormalizeFrequency(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return null;
        v = v.Trim().ToLowerInvariant();
        return v is "daily" or "weekly" or "monthly" ? v : null;
    }

    private static string? NormalizeLang(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return null;
        v = v.Trim().ToLowerInvariant();
        return v is "az" or "en" or "ru" or "tr" ? v : null;
    }

    private static NewsletterSubscriberDto Map(NewsletterSubscriber s)
    {
        var prefs = TryParsePrefs(s.Preferences) ?? DefaultPreferences();

        return new NewsletterSubscriberDto
        {
            Id = s.Id.ToString(),
            Email = s.Email,
            IsActive = s.IsActive,
            IsVerified = s.IsVerified,
            Preferences = prefs,
            Frequency = s.Frequency,
            PreferredLanguage = s.PreferredLanguage,
            SubscribedAt = s.SubscribedAt
        };
    }

    private static NewsletterPreferencesDto? TryParsePrefs(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<NewsletterPreferencesDto>(json, JsonOpts); }
        catch { return null; }
    }
}