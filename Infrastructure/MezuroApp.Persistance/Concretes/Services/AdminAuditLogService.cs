using System.Globalization;
using MongoDB.Driver;
using MezuroApp.Application.Dtos.Audit;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public sealed class AdminAuditLogService
{
    private readonly IMongoCollection<AuditLog> _col;
    private readonly UserManager<User> _userManager;

    public AdminAuditLogService(MongoDbContext ctx, UserManager<User> userManager)
    {
        _col = ctx.AuditLogs;
        _userManager = userManager;
    }

    public async Task<AdminAuditLogListResponseDto> GetAsync(AdminAuditLogFilterDto f, CancellationToken ct)
    {
        var fb = Builders<AuditLog>.Filter;
        var filter = fb.Empty;

        // --- AdminId
        if (!string.IsNullOrWhiteSpace(f.AdminId))
            filter &= fb.Eq(x => x.UserId, f.AdminId.Trim());

        // --- Module (EntityType)
        if (!string.IsNullOrWhiteSpace(f.Module) && !IsAll(f.Module))
        {
            var module = f.Module.Trim().ToLowerInvariant();
            filter &= fb.Eq(x => x.Module, module);
        }

        // --- Action (create/update/delete)
        if (!string.IsNullOrWhiteSpace(f.Action) && !IsAll(f.Action))
        {
            var act = f.Action.Trim().ToLowerInvariant();
            filter &= fb.Eq(x => x.ActionType, act);
        }

        // --- Date range (UTC) dd.MM.yyyy
        if (!string.IsNullOrWhiteSpace(f.From))
        {
            var fromUtc = ParseDdMmYyyyUtcOrThrow(f.From, "INVALID_FROM_DATE");
            filter &= fb.Gte(x => x.CreatedAt.AddHours(4), fromUtc);
        }

        if (!string.IsNullOrWhiteSpace(f.To))
        {
            var toExUtc = ParseDdMmYyyyUtcOrThrow(f.To, "INVALID_TO_DATE").AddDays(1);
            filter &= fb.Lt(x => x.CreatedAt.AddHours(4), toExUtc);
        }

        // --- Search (SearchText üstündən)
        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var s = f.Search.Trim().ToLowerInvariant();
            filter &= fb.Regex(x => x.SearchText, new MongoDB.Bson.BsonRegularExpression(s, "i"));
        }

        var page = Math.Max(1, f.Page);
        var size = Math.Clamp(f.PageSize, 1, 200);
        var skip = (page - 1) * size;

        var total = await _col.CountDocumentsAsync(filter, cancellationToken: ct);

        var logs = await _col.Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Limit(size)
            .ToListAsync(ct);

        // ✅ 1) Page-də olan unique admin id-ləri yığ
        var adminIds = logs
            .Select(x => x.UserId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        // ✅ 2) Batch şəkildə User-ları gətir (1 query)
        var adminDict = await LoadAdminsAsync(adminIds, ct);

        // ✅ 3) DTO map
        var items = logs.Select(x =>
        {
            adminDict.TryGetValue(x.UserId ?? "", out var adminInfo);

            return new AdminAuditLogListItemDto(
                Id: x.Id,
                AdminId: x.UserId ?? "unknown",
                AdminName: adminInfo?.FirstName ?? "",
                AdminSurname: adminInfo?.LastName ?? "",
                AdminEmail: adminInfo?.Email ?? "",
                EntityType: x.Module ?? "",
                Action: x.ActionType ?? "",
                IpAddress: x.IpAddress ?? "",
                UserAgent: x.UserAgent ?? "",
                CreatedAtUtc: x.CreatedAt.AddHours(4).ToString("dd.MM.yyyy HH:mm:ss"),
                OldValuesJson: x.OldValuesJson ?? new Dictionary<string, object>(),
                NewValuesJson: x.NewValuesJson ?? new Dictionary<string, object>()
            );
        }).ToList();

        return new AdminAuditLogListResponseDto(
            Items: items,
            Page: page,
            PageSize: size,
            TotalCount: total
        );
    }

    // ✅ Batch load helper
    private async Task<Dictionary<string, AdminMiniInfo>> LoadAdminsAsync(List<string> adminIds, CancellationToken ct)
    {
        var dict = new Dictionary<string, AdminMiniInfo>(StringComparer.OrdinalIgnoreCase);

        if (adminIds == null || adminIds.Count == 0)
            return dict;

        // string -> Guid parse
        var guids = adminIds
            .Select(id => Guid.TryParse(id, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .Distinct()
            .ToList();

        if (guids.Count == 0)
            return dict;

        var users = await _userManager.Users
            .AsNoTracking()
            .Where(u => guids.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email
            })
            .ToListAsync(ct);

        foreach (var u in users)
        {
            dict[u.Id.ToString()] = new AdminMiniInfo(
                u.FirstName ?? "",
                u.LastName ?? "",
                u.Email ?? ""
            );
        }

        return dict;
    }

    private sealed record AdminMiniInfo(string FirstName, string LastName, string Email);

    private static bool IsAll(string v)
        => v.Trim().Equals("all", StringComparison.OrdinalIgnoreCase);

    private static DateTime ParseDdMmYyyyUtcOrThrow(string v, string errKey)
    {
        const string fmt = "dd.MM.yyyy";
        if (!DateTime.TryParseExact(v.Trim(), fmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            throw new GlobalAppException(errKey);

        return DateTime.SpecifyKind(dt.Date, DateTimeKind.Utc);
    }
}