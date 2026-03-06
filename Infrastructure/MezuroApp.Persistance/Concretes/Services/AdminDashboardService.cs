using Microsoft.EntityFrameworkCore;
using MezuroApp.Application.Abstracts.Repositories.AbandonedCarts;
using MezuroApp.Application.Abstracts.Repositories.Categories;
using MezuroApp.Application.Abstracts.Repositories.Orders;
using MezuroApp.Application.Abstracts.Repositories.PaymentTransactions;
using MezuroApp.Application.Abstracts.Repositories.ProductCategories;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Dashboard;
using MezuroApp.Domain.Entities;

namespace MezuroApp.Persistance.Concretes.Services;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly IOrderReadRepository _orderRead;
    private readonly IPaymentTransactionReadRepository _trxRead;
    private readonly IAbandonedCartReadRepository _abandonedRead;
    private readonly IProductCategoryReadRepository _productCategoryRead;
    private readonly ICategoryReadRepository _categoryRead;

    public AdminDashboardService(
        IOrderReadRepository orderRead,
        IPaymentTransactionReadRepository trxRead,
        IAbandonedCartReadRepository abandonedRead,
        IProductCategoryReadRepository productCategoryRead,
        ICategoryReadRepository categoryRead)
    {
        _orderRead = orderRead;
        _trxRead = trxRead;
        _abandonedRead = abandonedRead;
        _productCategoryRead = productCategoryRead;
        _categoryRead = categoryRead;
    }

    public async Task<AdminDashboardDto> GetDashboardAsync(CancellationToken ct = default)
    {
        // =========================
        // LAST 7 DAYS (today included)
        // current: today-6 ... tomorrow(exclusive)
        // previous: today-13 ... today-6(exclusive)
        // =========================
        var today = DateTime.UtcNow.Date;

        var fromUtc = DateTime.SpecifyKind(today.AddDays(-6), DateTimeKind.Utc);
        var toExUtc = DateTime.SpecifyKind(today.AddDays(1), DateTimeKind.Utc);

        var prevFromUtc = DateTime.SpecifyKind(today.AddDays(-13), DateTimeKind.Utc);
        var prevToExUtc = DateTime.SpecifyKind(today.AddDays(-6), DateTimeKind.Utc);

        var ordersQ = _orderRead.Query()
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        var trxQ = _trxRead.Query()
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        var abandonedQ = _abandonedRead.Query()
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        var curOrders = ordersQ.Where(x => x.CreatedDate >= fromUtc && x.CreatedDate < toExUtc);
        var prevOrders = ordersQ.Where(x => x.CreatedDate >= prevFromUtc && x.CreatedDate < prevToExUtc);

        var curTrx = trxQ.Where(x => x.InitiatedAt >= fromUtc && x.InitiatedAt < toExUtc);
        var prevTrx = trxQ.Where(x => x.InitiatedAt >= prevFromUtc && x.InitiatedAt < prevToExUtc);

        var curAbandoned = abandonedQ.Where(x => x.CreatedDate >= fromUtc && x.CreatedDate < toExUtc);

        // =========================
        // SUMMARY
        // =========================
        var curRevenue = await curOrders
            .Where(x => x.PaymentStatus != null &&
                        (x.PaymentStatus.ToLower() == "paid" || x.PaymentStatus.ToLower() == "completed"))
            .SumAsync(x => (decimal?)x.Total, ct) ?? 0m;

        var prevRevenue = await prevOrders
            .Where(x => x.PaymentStatus != null &&
                        (x.PaymentStatus.ToLower() == "paid" || x.PaymentStatus.ToLower() == "completed"))
            .SumAsync(x => (decimal?)x.Total, ct) ?? 0m;

        var curOrderCount = await curOrders.CountAsync(ct);
        var prevOrderCount = await prevOrders.CountAsync(ct);

        var curTrxCount = await curTrx.CountAsync(ct);
        var prevTrxCount = await prevTrx.CountAsync(ct);

        var curSuccessCount = await curTrx.CountAsync(x =>
            x.Status != null &&
            (x.Status.ToLower() == "completed" || x.Status.ToLower() == "paid"), ct);

        var prevSuccessCount = await prevTrx.CountAsync(x =>
            x.Status != null &&
            (x.Status.ToLower() == "completed" || x.Status.ToLower() == "paid"), ct);

        var curRefundCount = await curTrx.CountAsync(x =>
            x.Status != null && x.Status.ToLower() == "refunded", ct);

        var prevRefundCount = await prevTrx.CountAsync(x =>
            x.Status != null && x.Status.ToLower() == "refunded", ct);

        var curSuccessRate = curTrxCount == 0
            ? 0m
            : Math.Round((decimal)curSuccessCount * 100m / curTrxCount, 2);

        var prevSuccessRate = prevTrxCount == 0
            ? 0m
            : Math.Round((decimal)prevSuccessCount * 100m / prevTrxCount, 2);

        var curRefundRate = curTrxCount == 0
            ? 0m
            : Math.Round((decimal)curRefundCount * 100m / curTrxCount, 2);

        var prevRefundRate = prevTrxCount == 0
            ? 0m
            : Math.Round((decimal)prevRefundCount * 100m / prevTrxCount, 2);

        var summary = new AdminDashboardSummaryDto(
            Revenue: curRevenue,
            Orders: curOrderCount,
            SuccessRate: curSuccessRate,
            RefundRate: curRefundRate,
            RevenueChangePercent: CalcChangePercent(curRevenue, prevRevenue),
            OrdersChangePercent: CalcChangePercent(curOrderCount, prevOrderCount),
            SuccessRateChangePercent: CalcChangePercent(curSuccessRate, prevSuccessRate),
            RefundRateChangePercent: CalcChangePercent(curRefundRate, prevRefundRate)
        );

        // =========================
        // REVENUE TREND (7 gün, boş gün = 0)
        // =========================
        var revenueTrendRaw = await curOrders
            .Where(x => x.PaymentStatus != null &&
                        (x.PaymentStatus.ToLower() == "paid" || x.PaymentStatus.ToLower() == "completed"))
            .GroupBy(x => x.CreatedDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                Revenue = g.Sum(x => x.Total)
            })
            .ToListAsync(ct);

        var revenueTrendMap = revenueTrendRaw.ToDictionary(x => x.Date, x => x.Revenue);

        var revenueTrend = Enumerable.Range(0, 7)
            .Select(i =>
            {
                var date = fromUtc.Date.AddDays(i);
                return new RevenueTrendItemDto(
                    date.ToString("dd.MM"),
                    revenueTrendMap.TryGetValue(date, out var revenue) ? revenue : 0m
                );
            })
            .ToList();

        // =========================
        // DAILY ORDERS (7 gün, boş gün = 0)
        // =========================
        var dailyOrdersRaw = await curOrders
            .GroupBy(x => x.CreatedDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                Count = g.Count()
            })
            .ToListAsync(ct);

        var dailyOrdersMap = dailyOrdersRaw.ToDictionary(x => x.Date, x => x.Count);

        var dailyOrders = Enumerable.Range(0, 7)
            .Select(i =>
            {
                var date = fromUtc.Date.AddDays(i);
                return new DailyOrdersItemDto(
                    date.ToString("dd.MM"),
                    dailyOrdersMap.TryGetValue(date, out var count) ? count : 0
                );
            })
            .ToList();

        // =========================
        // MONTHLY REFUNDS (burada da son 7 gün üçün)
        // boş gün = 0
        // =========================
        var refundRaw = await curTrx
            .Where(x => x.Status != null && x.Status.ToLower() == "refunded")
            .GroupBy(x => x.LastUpdatedDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                Count = g.Count()
            })
            .ToListAsync(ct);

        var refundMap = refundRaw.ToDictionary(x => x.Date, x => x.Count);

        var monthlyRefunds = Enumerable.Range(0, 7)
            .Select(i =>
            {
                var date = fromUtc.Date.AddDays(i);
                return new MonthlyRefundItemDto(
                    date.ToString("dd.MM"),
                    refundMap.TryGetValue(date, out var count) ? count : 0
                );
            })
            .ToList();

        // =========================
        // PAYMENT SUCCESS RATE
        // =========================
        var failedCount = await curTrx.CountAsync(x =>
            x.Status != null &&
            (x.Status.ToLower() == "failed" || x.Status.ToLower() == "cancelled"), ct);

        var paymentSuccessRate = new PaymentSuccessRateDto(
            SuccessfulCount: curSuccessCount,
            FailedCount: failedCount,
            SuccessRate: curSuccessRate
        );

        // =========================
        // TOP PRODUCTS
        // =========================
        var topProductsRaw = await curOrders
            .Include(x => x.OrderItems!.Where(i => !i.IsDeleted))
            .SelectMany(x => x.OrderItems!)
            .GroupBy(x => x.ProductName)
            .Select(g => new
            {
                ProductName = g.Key ?? "Unknown",
                Quantity = g.Sum(x => x.Quantity)
            })
            .OrderByDescending(x => x.Quantity)
            .Take(5)
            .ToListAsync(ct);

        var topProducts = topProductsRaw
            .Select(x => new TopProductItemDto(
                x.ProductName,
                x.Quantity
            ))
            .ToList();

        // =========================
        // TOP CATEGORIES
        // =========================
        var topCategoriesRaw = await curOrders
            .Include(x => x.OrderItems!.Where(i => !i.IsDeleted))
            .SelectMany(x => x.OrderItems!)
            .Join(
                _productCategoryRead.Query().AsNoTracking().Where(pc => !pc.IsDeleted),
                oi => oi.ProductId,
                pc => pc.ProductId,
                (oi, pc) => new { oi, pc }
            )
            .Join(
                _categoryRead.Query().AsNoTracking().Where(c => !c.IsDeleted),
                x => x.pc.CategoryId,
                c => c.Id,
                (x, c) => new
                {
                    CategoryName = c.NameAz,
                    Quantity = x.oi.Quantity
                }
            )
            .GroupBy(x => x.CategoryName)
            .Select(g => new
            {
                CategoryName = g.Key ?? "Unknown",
                Quantity = g.Sum(x => x.Quantity)
            })
            .OrderByDescending(x => x.Quantity)
            .Take(5)
            .ToListAsync(ct);

        var totalCategoryQty = topCategoriesRaw.Sum(x => x.Quantity);

        var topCategories = topCategoriesRaw
            .Select(x => new TopCategoryItemDto(
                x.CategoryName,
                x.Quantity,
                totalCategoryQty == 0 ? 0m : Math.Round((decimal)x.Quantity * 100m / totalCategoryQty, 2)
            ))
            .ToList();

        // =========================
        // ABANDONED CART FUNNEL
        // =========================
        var cartCreated = await curAbandoned.CountAsync(ct);

        var checkoutStarted = await curAbandoned.CountAsync(x =>
            x.Status != null &&
            (
                x.Status.ToLower() == "checkout_started" ||
                x.Status.ToLower() == "abandoned" ||
                x.Status.ToLower() == "recovered" ||
                x.Status.ToLower() == "created"
            ), ct);

        var completedOrders = await curAbandoned.CountAsync(x =>
            x.Status != null && x.Status.ToLower() == "recovered", ct);

        var conversionRate = cartCreated == 0
            ? 0m
            : Math.Round((decimal)completedOrders * 100m / cartCreated, 2);

        var abandonedCartFunnel = new AbandonedCartFunnelDto(
            CartCreated: cartCreated,
            CheckoutStarted: checkoutStarted,
            CompletedOrders: completedOrders,
            ConversionRate: conversionRate
        );

        // =========================
        // AVERAGE ORDER VALUE TREND (7 gün, boş gün = 0)
        // =========================
        var avgOrderRaw = await curOrders
            .Where(x => x.PaymentStatus != null &&
                        (x.PaymentStatus.ToLower() == "paid" || x.PaymentStatus.ToLower() == "completed"))
            .GroupBy(x => x.CreatedDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                AvgValue = g.Average(x => x.Total)
            })
            .ToListAsync(ct);

        var avgOrderMap = avgOrderRaw.ToDictionary(x => x.Date, x => Math.Round(x.AvgValue, 2));

        var avgOrderValueTrend = Enumerable.Range(0, 7)
            .Select(i =>
            {
                var date = fromUtc.Date.AddDays(i);
                return new AverageOrderValueItemDto(
                    date.ToString("dd.MM"),
                    avgOrderMap.TryGetValue(date, out var avg) ? avg : 0m
                );
            })
            .ToList();

        return new AdminDashboardDto(
            Summary: summary,
            RevenueTrend: revenueTrend,
            DailyOrders: dailyOrders,
            MonthlyRefunds: monthlyRefunds,
            PaymentSuccessRate: paymentSuccessRate,
            TopCategories: topCategories,
            TopProducts: topProducts,
            AbandonedCartFunnel: abandonedCartFunnel,
            AverageOrderValueTrend: avgOrderValueTrend
        );
    }

    private static decimal CalcChangePercent(decimal current, decimal previous)
    {
        if (previous == 0m)
        {
            if (current == 0m) return 0m;
            return 100m;
        }

        return Math.Round(((current - previous) / previous) * 100m, 2);
    }

    private static decimal CalcChangePercent(int current, int previous)
    {
        if (previous == 0)
        {
            if (current == 0) return 0m;
            return 100m;
        }

        return Math.Round(((decimal)(current - previous) / previous) * 100m, 2);
    }
}