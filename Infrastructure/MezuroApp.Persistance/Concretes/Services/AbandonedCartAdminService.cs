using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MezuroApp.Application.Abstracts.Repositories.AbandonedCarts;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.AbandonedCart;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;
using MezuroApp.Domain.HelperEntities;

public sealed class AbandonedCartAdminService : IAbandonedCartAdminService
{
    private readonly IAbandonedCartReadRepository _readRepo;
    private readonly IAbandonedCartWriteRepository _writeRepo; // hələlik istifadə etməsək də inject edirik

    public AbandonedCartAdminService(
        IAbandonedCartReadRepository readRepo,
        IAbandonedCartWriteRepository writeRepo)
    {
        _readRepo = readRepo;
        _writeRepo = writeRepo;
    }

    public async Task<AbandonedCartStatsDto> GetStatsAsync(AbandonedCartAdminFilter filter)
    {
        var now = DateTime.UtcNow;

        var q = ApplyFilter(_readRepo.Query().AsNoTracking(), filter, now);

        var total = await q.CountAsync();
        var potential = await q.SumAsync(x => (decimal?)(x.TotalAmount ?? 0m)) ?? 0m;

        return new AbandonedCartStatsDto
        {
            TotalAbandonedCarts = total,
            PotentialRevenue = potential
        };
    }

    public async Task<PagedResult<AbandonedCartListItemDto>> GetPagedAsync(
        AbandonedCartAdminFilter filter,
        int page,
        int pageSize)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;

        var now = DateTime.UtcNow;

        var baseQ = ApplyFilter(_readRepo.Query().AsNoTracking(), filter, now);

        var total = await baseQ.CountAsync();

        var items = await baseQ
            .OrderByDescending(x => x.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.Email,
                x.UserId,
                x.FootprintId,
                x.Status,
                x.CreatedDate,
                x.ExpiresAt,
                x.TotalAmount,
                x.RecoveryEmailSent,
                x.CartItemsJson
            })
            .ToListAsync();

        var mapped = items.Select(x =>
        {
            var count = SafeCountItems(x.CartItemsJson);

            return new AbandonedCartListItemDto
            {
                Id = x.Id.ToString(),
                Email = x.Email,
                IsGuest = x.UserId == null,
                Status = x.Status,
                CreatedAt = x.CreatedDate.ToString("dd.MM.yyyy HH:mm"),
                ExpiryDate = x.ExpiresAt?.ToString("dd.MM.yyyy"),
                TotalAmount = x.TotalAmount ?? 0m,
                ItemsCount = count,
                RecoveryEmailSent = x.RecoveryEmailSent
            };
        }).ToList();

        return new PagedResult<AbandonedCartListItemDto>
        {
            Items = mapped,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<AbandonedCartDetailDto> GetDetailAsync(string id)
    {
        if (!Guid.TryParse(id, out var gid))
            throw new GlobalAppException("INVALID_ABANDONED_CART_ID");

        var entity = await _readRepo.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == gid);

        if (entity == null)
            throw new GlobalAppException("ABANDONED_CART_NOT_FOUND");

        var items = SafeParseItems(entity.CartItemsJson);

        return new AbandonedCartDetailDto
        {
            Id = entity.Id.ToString(),
            Email = entity.Email,
            Status = entity.Status,
            CreatedAt = entity.CreatedDate.ToString("dd.MM.yyyy HH:mm"),
            ExpiryDate = entity.ExpiresAt?.ToString("dd.MM.yyyy"),
            TotalAmount = entity.TotalAmount ?? 0m,

            UserId = entity.UserId?.ToString(),
            FootprintId = entity.FootprintId,
            BasketId = entity.BasketId?.ToString(),

            RecoveryEmailSent = entity.RecoveryEmailSent,
            RecoveryEmailSentAt = entity.RecoveryEmailSentAt?.ToString("dd.MM.yyyy HH:mm"),

            ConvertedToOrderId = entity.ConvertedToOrderId?.ToString(),

            Items = items.Select(i => new AbandonedCartItemDto
            {
                ProductId = i.ProductId.ToString(),
                ProductVariantId = i.ProductVariantId?.ToString(),
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.UnitPrice * i.Quantity
            }).ToList()
        };
    }

    // -------------------------
    // Filter
    // -------------------------
    private static IQueryable<AbandonedCart> ApplyFilter(
        IQueryable<AbandonedCart> q,
        AbandonedCartAdminFilter filter,
        DateTime nowUtc)
    {
        q = q.Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim().ToLowerInvariant();
            q = q.Where(x =>
                (x.Email != null && x.Email.ToLower().Contains(s)) ||
                (x.FootprintId != null && x.FootprintId.ToLower().Contains(s))
            );
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            var st = filter.Status.Trim().ToLowerInvariant();
            q = q.Where(x => x.Status.ToLower() == st);
        }

        var createdFrom = ParseDate(filter.CreatedFrom, "INVALID_CREATED_FROM_DATE");
        var createdUntil = ParseDate(filter.CreatedUntil, "INVALID_CREATED_UNTIL_DATE", endOfDay: true);

        if (createdFrom.HasValue) q = q.Where(x => x.CreatedDate >= createdFrom.Value);
        if (createdUntil.HasValue) q = q.Where(x => x.CreatedDate <= createdUntil.Value);

        var expFrom = ParseDate(filter.ExpiryFrom, "INVALID_EXPIRY_FROM_DATE");
        var expUntil = ParseDate(filter.ExpiryUntil, "INVALID_EXPIRY_UNTIL_DATE", endOfDay: true);

        if (expFrom.HasValue) q = q.Where(x => x.ExpiresAt != null && x.ExpiresAt >= expFrom.Value);
        if (expUntil.HasValue) q = q.Where(x => x.ExpiresAt != null && x.ExpiresAt <= expUntil.Value);

        if (filter.Recoverable == true)
        {
            q = q.Where(x =>
                x.Status.ToLower() !="recovered" &&
                (x.ExpiresAt == null || x.ExpiresAt > nowUtc)
            );
        }

        return q;
    }

    private static DateTime? ParseDate(string? value, string errorKey, bool endOfDay = false)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        if (!DateTime.TryParseExact(value.Trim(), "dd.MM.yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            throw new GlobalAppException(errorKey);

        var dt = d.Date;
        if (endOfDay) dt = dt.AddDays(1).AddTicks(-1);
        return dt;
    }

    // -------------------------
    // JSON parse helpers
    // -------------------------
    private sealed class AbandonedCartItemSnapshot
    {
        public Guid ProductId { get; set; }
        public Guid? ProductVariantId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    private static int SafeCountItems(string json)
    {
        try
        {
            var list = JsonSerializer.Deserialize<List<AbandonedCartItemSnapshot>>(json);
            return list?.Sum(x => Math.Max(0, x.Quantity)) ?? 0;
        }
        catch { return 0; }
    }

    private static List<AbandonedCartItemSnapshot> SafeParseItems(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<AbandonedCartItemSnapshot>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }
}