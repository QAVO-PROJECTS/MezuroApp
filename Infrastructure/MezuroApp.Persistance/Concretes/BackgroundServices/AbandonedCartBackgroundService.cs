using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MezuroApp.Application.Abstracts.Repositories.AbandonedCarts;
using MezuroApp.Application.Abstracts.Repositories.Baskets;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Domain.Entities;
using MezuroApp.Domain.HelperEntities;

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
                await SendRecoveryEmailsAsync(stoppingToken);

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
    private async Task SendRecoveryEmailsAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();

        var abandonedRead = scope.ServiceProvider.GetRequiredService<IAbandonedCartReadRepository>();
        var abandonedWrite = scope.ServiceProvider.GetRequiredService<IAbandonedCartWriteRepository>();
        var emailService = scope.ServiceProvider.GetRequiredService<IMailService>(); // səndə hansıdırsa

        var now = DateTime.UtcNow;

        var carts = await abandonedRead.GetAllAsync(
            x => !x.IsDeleted
                 && x.Status == "created"
                 && !x.RecoveryEmailSent
                 && x.Email != null
                 && (x.ExpiresAt == null || x.ExpiresAt > now),
            enableTracking: true
        );

        if (carts.Count == 0)
            return;

        foreach (var cart in carts)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                // 🔹 Recovery link (sənin frontend URL-inə görə dəyiş)
                var recoveryLink = $"https://mezuro.az/recover-cart/{cart.Id}";

                var subject = "Səbətiniz sizi gözləyir 🛒";
                var body = $@"
                Salam,

                Seçdiyiniz məhsullar səbətinizdə qalır.
                Sifarişi tamamlamaq üçün linkə daxil olun:

                {recoveryLink}

                Hörmətlə,
                Mezuro
            ";

                MailRequest mailRequest = new MailRequest()
                {

                    Body = body,
                    Subject = subject,
                    ToEmail =  cart.Email,
                };
                await emailService.SendEmailAsync(mailRequest);

                // ✅ STATUS UPDATE
                cart.Status ="sent";
                cart.RecoveryEmailSent = true;
                cart.RecoveryEmailSentAt = now;
                cart.LastUpdatedDate = now;

                await abandonedWrite.UpdateAsync(cart);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Recovery email sending failed for cart {CartId}", cart.Id);
            }
        }

        await abandonedWrite.CommitAsync();
    }

private async Task ScanAndSnapshotAsync(CancellationToken ct)
{
    using var scope = _sp.CreateScope();

    var basketRead = scope.ServiceProvider.GetRequiredService<IBasketReadRepository>();
    var abandonedRead = scope.ServiceProvider.GetRequiredService<IAbandonedCartReadRepository>();
    var abandonedWrite = scope.ServiceProvider.GetRequiredService<IAbandonedCartWriteRepository>();

    var now = DateTime.UtcNow;
    var cutoffUtc = now - _inactiveAfter;

    // 0) Expire olanları soft-delete (opsional amma yaxşıdır)
    var expired = await abandonedRead.GetAllAsync(a =>
        !a.IsDeleted && a.ExpiresAt != null && a.ExpiresAt <= now
    );
    foreach (var x in expired)
    {
        x.IsDeleted = true;
        x.DeletedDate = now;
        x.LastUpdatedDate = now;
        await abandonedWrite.UpdateAsync(x);
    }
    if (expired.Count > 0) await abandonedWrite.CommitAsync();

    // 1) 24 saat inactive olan basketlər
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

    if (baskets.Count == 0) return;

    int created = 0;

    foreach (var basket in baskets)
    {
        ct.ThrowIfCancellationRequested();

        // basket artıq boşalıbsa skip
        if (basket.BasketItems == null || basket.BasketItems.All(i => i.IsDeleted))
            continue;

        // 2) Eyni basket+same lastUpdated snapshot varsa skip
        var exists = await abandonedRead.GetAsync(a =>
            !a.IsDeleted
            && a.BasketId == basket.Id
            && a.BasketLastUpdatedSnapshotUtc == basket.LastUpdatedDate
        );

        if (exists != null) continue;

        // 3) Snapshot
        var items = basket.BasketItems
            .Where(i => !i.IsDeleted)
            .Select(i =>
            {
                var productPrice = i.Product?.Price ?? 0m;
                var modifier = i.ProductVariantId != null && i.ProductVariant != null
                    ? i.ProductVariant.PriceModifier
                    : 0m;

                return new AbandonedCartItemSnapshot
                {
                    ProductId = i.ProductId,
                    ProductVariantId = i.ProductVariantId,
                    Quantity = i.Quantity,
                    UnitPrice = productPrice + modifier
                };
            })
            .Where(x => x.Quantity > 0)
            .ToList();

        if (items.Count == 0) continue;

        var total = items.Sum(x => x.UnitPrice * x.Quantity);

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

            Status = "created",
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
}

    private sealed class AbandonedCartItemSnapshot
    {
        public Guid ProductId { get; set; }
        public Guid? ProductVariantId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}