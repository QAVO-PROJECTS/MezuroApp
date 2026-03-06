using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MezuroApp.Application.Abstracts.Repositories.AbandonedCarts;
using MezuroApp.Application.Abstracts.Repositories.Orders;
using MezuroApp.Application.Abstracts.Repositories.PaymentTransactions;
using MezuroApp.Application.Abstracts.Repositories.Products;
using MezuroApp.Application.Abstracts.Repositories.ProductCategories;
using MezuroApp.Application.Abstracts.Repositories.Categories;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Dashboard;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;
namespace MezuroApp.Persistance.Concretes.Services;

public class AdminDashboardService:IAdminDashboardService
{
    private readonly IOrderReadRepository _orderRead;
    private readonly IPaymentTransactionReadRepository _trxRead;
    private readonly IAbandonedCartReadRepository _abandonedRead;
    private readonly IProductReadRepository _productRead;
    private readonly IProductCategoryReadRepository _productCategoryRead;
    private readonly ICategoryReadRepository _categoryRead;

    public AdminDashboardService(
        IOrderReadRepository orderRead,
        IPaymentTransactionReadRepository trxRead,
        IAbandonedCartReadRepository abandonedRead,
        IProductReadRepository productRead,
        IProductCategoryReadRepository productCategoryRead,
        ICategoryReadRepository categoryRead)
    {
        _orderRead = orderRead;
        _trxRead = trxRead;
        _abandonedRead = abandonedRead;
        _productRead = productRead;
        _productCategoryRead = productCategoryRead;
        _categoryRead = categoryRead;
    }
        public async Task<AdminDashboardDto> GetDashboardAsync(AdminDashboardFilterDto filter, CancellationToken ct = default)
    {
        var (fromUtc, toExUtc, prevFromUtc, prevToExUtc) = ResolveRanges(filter);

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
            .Where(x => x.PaymentStatus == "paid" || x.PaymentStatus == "completed")
            .SumAsync(x => (decimal?)x.Total, ct) ?? 0m;

        var prevRevenue = await prevOrders
            .Where(x => x.PaymentStatus == "paid" || x.PaymentStatus == "completed")
            .SumAsync(x => (decimal?)x.Total, ct) ?? 0m;

        var curOrderCount = await curOrders.CountAsync(ct);
        var prevOrderCount = await prevOrders.CountAsync(ct);

        var curTrxCount = await curTrx.CountAsync(ct);
        var prevTrxCount = await prevTrx.CountAsync(ct);

        var curSuccessCount = await curTrx.CountAsync(x =>
            x.Status == "completed" || x.Status == "paid", ct);

        var prevSuccessCount = await prevTrx.CountAsync(x =>
            x.Status == "completed" || x.Status == "paid", ct);

        var curRefundCount = await curTrx.CountAsync(x => x.Status == "refunded", ct);
        var prevRefundCount = await prevTrx.CountAsync(x => x.Status == "refunded", ct);

        var curSuccessRate = curTrxCount == 0 ? 0m : Math.Round((decimal)curSuccessCount * 100m / curTrxCount, 2);
        var prevSuccessRate = prevTrxCount == 0 ? 0m : Math.Round((decimal)prevSuccessCount * 100m / prevTrxCount, 2);

        var curRefundRate = curTrxCount == 0 ? 0m : Math.Round((decimal)curRefundCount * 100m / curTrxCount, 2);
        var prevRefundRate = prevTrxCount == 0 ? 0m : Math.Round((decimal)prevRefundCount * 100m / prevTrxCount, 2);

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
        // REVENUE TREND
        // =========================
        var revenueTrendRaw = await curOrders
            .Where(x => x.PaymentStatus == "paid" || x.PaymentStatus == "completed")
            .GroupBy(x => x.CreatedDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                Revenue = g.Sum(x => x.Total)
            })
            .OrderBy(x => x.Date)
            .ToListAsync(ct);

        var revenueTrend = revenueTrendRaw
            .Select(x => new RevenueTrendItemDto(
                x.Date.ToString("dd.MM"),
                x.Revenue
            ))
            .ToList();

        // =========================
        // DAILY ORDERS
        // =========================
        var dailyOrdersRaw = await curOrders
            .GroupBy(x => x.CreatedDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToListAsync(ct);

        var dailyOrders = dailyOrdersRaw
            .Select(x => new DailyOrdersItemDto(
                x.Date.ToString("dd.MM"),
                x.Count
            ))
            .ToList();

        // =========================
        // MONTHLY REFUNDS
        // =========================
        var monthlyRefundRaw = await curTrx
            .Where(x => x.Status == "refunded")
            .GroupBy(x => x.LastUpdatedDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToListAsync(ct);

        var monthlyRefunds = monthlyRefundRaw
            .Select(x => new MonthlyRefundItemDto(
                x.Date.ToString("dd.MM"),
                x.Count
            ))
            .ToList();

        // =========================
        // PAYMENT SUCCESS RATE
        // =========================
        var failedCount = await curTrx.CountAsync(x =>
            x.Status == "failed" || x.Status == "cancelled", ct);

        var paymentSuccessRate = new PaymentSuccessRateDto(
            SuccessfulCount: curSuccessCount,
            FailedCount: failedCount,
            SuccessRate: curSuccessRate
        );

        // =========================
        // TOP PRODUCTS
        // =========================
        var topProducts = await curOrders
            .Include(x => x.OrderItems!.Where(i => !i.IsDeleted))
            .SelectMany(x => x.OrderItems!)
            .GroupBy(x => x.ProductName)
            .Select(g => new TopProductItemDto(
                g.Key ?? "Unknown",
                g.Sum(x => x.Quantity)
            ))
            .OrderByDescending(x => x.Quantity)
            .Take(5)
            .ToListAsync(ct);

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
            x.Status == "checkout_started" || x.Status == "abandoned" || x.Status == "recovered" || x.Status=="created", ct);

        var completedOrders = await curAbandoned.CountAsync(x =>
            x.Status == "recovered", ct);

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
        // AVERAGE ORDER VALUE
        // =========================
        var avgOrderRaw = await curOrders
            .Where(x => x.PaymentStatus == "paid" || x.PaymentStatus == "completed")
            .GroupBy(x => x.CreatedDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                AvgValue = g.Average(x => x.Total)
            })
            .OrderBy(x => x.Date)
            .ToListAsync(ct);

        var avgOrderValueTrend = avgOrderRaw
            .Select(x => new AverageOrderValueItemDto(
                x.Date.ToString("dd.MM"),
                Math.Round(x.AvgValue, 2)
            ))
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
    //Helper Methods
        private static (DateTime curFrom, DateTime curToEx, DateTime prevFrom, DateTime prevToEx)
        ResolveRanges(AdminDashboardFilterDto filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.From) || !string.IsNullOrWhiteSpace(filter.To))
        {
            var curFrom = !string.IsNullOrWhiteSpace(filter.From)
                ? ParseDdMmYyyyUtcOrThrow(filter.From, "INVALID_FROM_DATE")
                : DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

            var curToEx = !string.IsNullOrWhiteSpace(filter.To)
                ? ParseDdMmYyyyUtcOrThrow(filter.To, "INVALID_TO_DATE").AddDays(1)
                : DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(1), DateTimeKind.Utc);

            if (curToEx <= curFrom)
                throw new GlobalAppException("INVALID_DATE_RANGE");

            var span = curToEx - curFrom;
            var prevToEx = curFrom;
            var prevFrom = curFrom - span;

            return (curFrom, curToEx, prevFrom, prevToEx);
        }

        var now = DateTime.UtcNow;
        var curFromMtd = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var curToExMtd = DateTime.SpecifyKind(now.Date.AddDays(1), DateTimeKind.Utc);

        var prevMonth = now.AddMonths(-1);
        var prevFromMtd = new DateTime(prevMonth.Year, prevMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var elapsed = curToExMtd - curFromMtd;
        var prevToExMtd = prevFromMtd + elapsed;

        return (curFromMtd, curToExMtd, prevFromMtd, prevToExMtd);
    }

    private static DateTime ParseDdMmYyyyUtcOrThrow(string value, string errorKey)
    {
        if (!DateTime.TryParseExact(
                value.Trim(),
                "dd.MM.yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dt))
            throw new GlobalAppException(errorKey);

        return DateTime.SpecifyKind(dt.Date, DateTimeKind.Utc);
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