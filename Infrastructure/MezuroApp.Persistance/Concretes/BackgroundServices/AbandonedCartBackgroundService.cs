using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MezuroApp.Application.Abstracts.Repositories.AbandonedCarts;
using MezuroApp.Application.Abstracts.Repositories.Baskets;
using MezuroApp.Domain.Entities;

namespace MezuroApp.Persistance.Concretes.BackgroundServices;

public sealed class AbandonedCartBackgroundService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<AbandonedCartBackgroundService> _log;
    private readonly TimeSpan _checkEvery;
    private readonly TimeSpan _inactiveAfter;
    private readonly int _expireDays;

    public AbandonedCartBackgroundService(
        IServiceProvider sp,
        IConfiguration cfg,
        ILogger<AbandonedCartBackgroundService> log)
    {
        _sp = sp;
        _log = log;

        _checkEvery = TimeSpan.FromMinutes(cfg.GetValue<int>("AbandonedCart:CheckEveryMinutes", 10));
        _inactiveAfter = TimeSpan.FromMinutes(cfg.GetValue<int>("AbandonedCart:InactiveAfterMinutes", 60));
        _expireDays = cfg.GetValue<int>("AbandonedCart:ExpireDays", 30);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // ilk startda bir az gecikmə (app ayağa qalxsın)
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ScanAndSnapshotAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // normal shutdown
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "AbandonedCartBackgroundService scan error (loop continues).");
            }

            await Task.Delay(_checkEvery, stoppingToken);
        }
    }

    private async Task ScanAndSnapshotAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();

        var basketRead = scope.ServiceProvider.GetRequiredService<IBasketReadRepository>();
        var abandonedRead = scope.ServiceProvider.GetRequiredService<IAbandonedCartReadRepository>();
        var abandonedWrite = scope.ServiceProvider.GetRequiredService<IAbandonedCartWriteRepository>();

        var now = DateTime.UtcNow;
        var cutoffUtc = now - _inactiveAfter;

        // 1) “tərk edilmiş” basketləri tapırıq:
        // - silinməyib
        // - lastUpdated cutoff-dan köhnədir
        // - içində ən az 1 aktiv item var
        var baskets = await basketRead.GetAllAsync(
            b => !b.IsDeleted
                 && b.LastUpdatedDate <= cutoffUtc
                 && b.BasketItems.Any(i => !i.IsDeleted),
            include: q => q
                .Include(b => b.BasketItems.Where(i => !i.IsDeleted))
                    .ThenInclude(i => i.Product)
                .Include(b => b.BasketItems.Where(i => !i.IsDeleted))
                    .ThenInclude(i => i.ProductVariant)
                .AsSplitQuery(),
            enableTracking: false
        );

        if (baskets.Count == 0)
        {
            _log.LogInformation("AbandonedCart scan: no candidates. cutoffUtc={CutoffUtc}", cutoffUtc);
            return;
        }

        _log.LogInformation("AbandonedCart scan: candidates={Count}, cutoffUtc={CutoffUtc}", baskets.Count, cutoffUtc);

        int created = 0;

        foreach (var basket in baskets)
        {
            ct.ThrowIfCancellationRequested();

            // 2) Təkrarlanmasın deyə yoxlayırıq:
            // EYNİ basket üçün, EYNİ lastUpdatedDate ilə snapshot artıq varsa -> skip
            var exists = await abandonedRead.GetAsync(a =>
                !a.IsDeleted
                && a.BasketId == basket.Id
                && a.BasketLastUpdatedSnapshotUtc == basket.LastUpdatedDate
            );

            if (exists != null)
                continue;

            // 3) Snapshot items
            var items = (basket.BasketItems ?? new List<BasketItem>())
                .Where(i => !i.IsDeleted)
                .Select(i =>
                {
                    var productPrice = i.Product?.Price ?? 0m;
                    var modifier = (i.ProductVariantId != null && i.ProductVariant != null)
                        ? (i.ProductVariant.PriceModifier)
                        : 0m;

                    var unitPrice = productPrice + modifier;

                    return new AbandonedCartItemSnapshot
                    {
                        ProductId = i.ProductId,
                        ProductVariantId = i.ProductVariantId,
                        Quantity = i.Quantity,
                        UnitPrice = unitPrice
                    };
                })
                .ToList();

            if (items.Count == 0)
                continue;

            var total = items.Sum(x => x.UnitPrice * x.Quantity);

            // 4) AbandonedCart yazırıq (email lazım deyil — analitika üçün)
            var snapshot = new AbandonedCart
            {
                Id = Guid.NewGuid(),

                UserId = basket.UserId,
                FootprintId = basket.FootprintId,

                BasketId = basket.Id,

                Email = null,

                CartItemsJson = JsonSerializer.Serialize(items),
                TotalAmount = total,

                BasketLastUpdatedSnapshotUtc = basket.LastUpdatedDate,

                RecoveryEmailSent = false,
                RecoveryEmailSentAt = null,

                ExpiresAt = now.AddDays(_expireDays),

                CreatedDate = now,
                LastUpdatedDate = now,
                IsDeleted = false
            };

            await abandonedWrite.AddAsync(snapshot);
            created++;
        }

        if (created > 0)
            await abandonedWrite.CommitAsync();

        _log.LogInformation("AbandonedCart scan done. created={Created}", created);
    }

    private sealed class AbandonedCartItemSnapshot
    {
        public Guid ProductId { get; set; }
        public Guid? ProductVariantId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}