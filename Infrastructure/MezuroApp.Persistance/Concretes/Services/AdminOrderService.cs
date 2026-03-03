using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MezuroApp.Application.Abstracts.Repositories.Orders;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Order.AdminOrder;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;
using MezuroApp.Domain.HelperEntities;

public sealed class AdminOrderService : IAdminOrderService
{
    private readonly IOrderReadRepository _orderRead;
    private readonly IOrderWriteRepository _orderWrite;
    private readonly IEmailCampaignService _campaign;

    public AdminOrderService(
        IOrderReadRepository orderRead,
        IOrderWriteRepository orderWrite,
        IEmailCampaignService campaign)
    {
        _orderRead = orderRead;
        _orderWrite = orderWrite;
        _campaign = campaign;
    }

    private static DateTime ParseDdMmYyyyOrThrow(string value, string errorKey)
    {
        if (!DateTime.TryParseExact(
                value.Trim(),
                "dd.MM.yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dt))
            throw new GlobalAppException(errorKey);

        // ⚠️ vacib hissə
        return DateTime.SpecifyKind(dt.Date, DateTimeKind.Utc);
    }
 public async Task<PagedResult<AdminOrderListItemDto>> GetOrdersAsync(AdminOrdersFilterDto f, CancellationToken ct)
{
    var q = _orderRead.Query()
        .AsNoTracking()
        .Where(o => !o.IsDeleted);

    if (!string.IsNullOrWhiteSpace(f.Search))
    {
        var s = f.Search.Trim().ToLowerInvariant();
        q = q.Where(o =>
            o.OrderNumber.ToLower().Contains(s) ||
            o.Email.ToLower().Contains(s) ||
            (o.Phone != null && o.Phone.ToLower().Contains(s)));
    }

    if (!string.IsNullOrWhiteSpace(f.Status))
    {
        var st = f.Status.Trim().ToLowerInvariant();
        q = q.Where(o => (o.Status ?? "").ToLower() == st);
    }

    if (!string.IsNullOrWhiteSpace(f.PaymentStatus))
    {
        var ps = f.PaymentStatus.Trim().ToLowerInvariant();
        q = q.Where(o => (o.PaymentStatus ?? "").ToLower() == ps);
    }

    if (!string.IsNullOrWhiteSpace(f.FulfillmentStatus))
    {
        var fs = f.FulfillmentStatus.Trim().ToLowerInvariant();
        q = q.Where(o => (o.FulfillmentStatus ?? "").ToLower() == fs);
    }

    // ✅ dd.MM.yyyy -> DateTime parse + filter
    if (!string.IsNullOrWhiteSpace(f.FromUtc))
    {
        var fromDate = ParseDdMmYyyyOrThrow(f.FromUtc, "INVALID_FROM_DATE");
        q = q.Where(o => o.CreatedDate >= fromDate);
    }

    if (!string.IsNullOrWhiteSpace(f.ToUtc))
    {
        var toDate = ParseDdMmYyyyOrThrow(f.ToUtc, "INVALID_TO_DATE");
        var toExclusive = toDate.AddDays(1); // inclusive day filter
        q = q.Where(o => o.CreatedDate < toExclusive);
    }

    if (f.MinAmount.HasValue) q = q.Where(o => o.Total >= f.MinAmount.Value);
    if (f.MaxAmount.HasValue) q = q.Where(o => o.Total <= f.MaxAmount.Value);

    var total = await q.CountAsync(ct);

    var page = Math.Max(1, f.Page);
    var size = Math.Clamp(f.PageSize, 1, 200);

    // ⚠️ dd.MM.yyyy format response üçün ToString EF-də çevrilmir, ona görə 2 mərhələ
    var raw = await q
        .OrderByDescending(o => o.CreatedDate)
        .Skip((page - 1) * size)
        .Take(size)
        .Select(o => new
        {
            o.Id,
            o.OrderNumber,
            o.FirstName,
            o.LastName,
            o.Total,
            o.PaymentStatus,
            o.FulfillmentStatus,
            o.Status,
            o.CreatedDate
        })
        .ToListAsync(ct);

    var items = raw.Select(o => new AdminOrderListItemDto(
        o.Id,
        o.OrderNumber,
        (((o.FirstName ?? "") + " " + (o.LastName ?? "")).Trim()),
        o.Total,
        "AZN",
        o.PaymentStatus ?? "pending",
        o.FulfillmentStatus ?? "unfulfilled",
        o.Status ?? "pending",
        o.CreatedDate.ToString("dd.MM.yyyy") // ✅ admin panelə belə gedəcək
    )).ToList();

    return new PagedResult<AdminOrderListItemDto>
    {
        Items = items,
        TotalCount = total,
        Page = page,
        PageSize = size
    };
}

    public async Task<AdminOrderDetailDto> GetOrderDetailAsync(string orderId, CancellationToken ct)
    {
        if (!Guid.TryParse(orderId, out var oid))
            throw new GlobalAppException("INVALID_ORDER_ID");

        var order = await _orderRead.Query()
            .AsNoTracking()
            .Where(o => !o.IsDeleted && o.Id == oid)
            .Include(o => o.OrderItems!.Where(i => !i.IsDeleted))
            .Include(o => o.PaymentTransactions!.Where(t => !t.IsDeleted))
            .FirstOrDefaultAsync(ct);

        if (order == null)
            throw new GlobalAppException("ORDER_NOT_FOUND");

        var customer = new AdminOrderCustomerDto(
            (((order.FirstName ?? "") + " " + (order.LastName ?? "")).Trim()),
            order.Email,
            order.Phone
        );

        var items = (order.OrderItems ?? new List<OrderItem>())
            .Select(i => new AdminOrderItemDto(
                i.Id,
                i.ProductName,
                i.ProductSku,
                i.ProductVariantName,
                i.Quantity,
                i.UnitPrice,
                i.DiscountAmount ?? 0m,
                i.Total,
                i.ProductImageUrl
            ))
            .ToList();

        var txs = (order.PaymentTransactions ?? new List<PaymentTransaction>())
            .OrderByDescending(t => t.InitiatedAt)
            .Select(t => new AdminOrderTransactionDto(
                t.Id,
                t.Status,
                t.Amount,
                t.RefundedAmount,
                t.Currency,
                t.PaymentMethod,
                t.TransactionId,
                t.InitiatedAt
            ))
            .ToList();

        // ✅ Refund entity YOX: refunds tab = transactions where RefundedAmount > 0
        var refunds = (order.PaymentTransactions ?? new List<PaymentTransaction>())
            .Where(t => t.RefundedAmount > 0m)
            .OrderByDescending(t => t.LastUpdatedDate)
            .Select(t => new AdminRefundListItemDto(
                t.Id,
                order.Id,
                order.OrderNumber,
                t.Amount,
                t.RefundedAmount,
                t.Currency,
                (t.RefundedAmount >= t.Amount) ? "refunded" : "partial_refunded",
                t.LastUpdatedDate
            ))
            .ToList();

        return new AdminOrderDetailDto(
            order.Id,
            order.OrderNumber,
            order.CreatedDate,
            "AZN",
            order.SubTotal,
            order.DiscountAmount ?? 0m,
            order.ShippingCost ?? 0m,
            order.TaxAmount ?? 0m,
            order.Total,
            order.PaymentMethod ?? "unknown",
            order.PaymentStatus ?? "pending",
            order.FulfillmentStatus ?? "unfulfilled",
            order.Status ?? "pending",
            customer,
            items,
            txs,
            refunds
        );
    }

    public async Task SetOrderStatusAsync(string orderId, string newStatus, CancellationToken ct)
    {
        if (!Guid.TryParse(orderId, out var oid))
            throw new GlobalAppException("INVALID_ORDER_ID");

        var s = (newStatus ?? "").Trim().ToLowerInvariant();
        if (s is not ("pending" or "processing" or "shipped" or "delivered" or "cancelled"))
            throw new GlobalAppException("INVALID_STATUS");

        var order = await _orderRead.GetAsync(o => !o.IsDeleted && o.Id == oid, enableTracking: true);
        if (order == null)
            throw new GlobalAppException("ORDER_NOT_FOUND");

        order.Status = s;
        order.LastUpdatedDate = DateTime.UtcNow;

        if (s == "cancelled") order.CancelledDate = DateTime.UtcNow;
        if (s == "shipped") order.ShippedDate = DateTime.UtcNow;
        if (s == "delivered") order.DeliveredDate = DateTime.UtcNow;

        await _orderWrite.UpdateAsync(order);
        await _orderWrite.CommitAsync();

        await _campaign.CreateAndScheduleOrderStatusCampaignAsync(order);
    }

    public async Task CancelOrderAsync(string orderId, string? adminNote, CancellationToken ct)
    {
        if (!Guid.TryParse(orderId, out var oid))
            throw new GlobalAppException("INVALID_ORDER_ID");

        var order = await _orderRead.GetAsync(o => !o.IsDeleted && o.Id == oid, enableTracking: true);
        if (order == null)
            throw new GlobalAppException("ORDER_NOT_FOUND");

        order.Status = "cancelled";
        order.CancelledDate = DateTime.UtcNow;
        order.AdminNote = adminNote;
        order.LastUpdatedDate = DateTime.UtcNow;

        await _orderWrite.UpdateAsync(order);
        await _orderWrite.CommitAsync();

        await _campaign.CreateAndScheduleOrderStatusCampaignAsync(order);
    }

    public async Task ResendConfirmationAsync(string orderId, CancellationToken ct)
    {
        if (!Guid.TryParse(orderId, out var oid))
            throw new GlobalAppException("INVALID_ORDER_ID");

        var order = await _orderRead.GetAsync(o => !o.IsDeleted && o.Id == oid, enableTracking: false);
        if (order == null)
            throw new GlobalAppException("ORDER_NOT_FOUND");

        await _campaign.CreateAndScheduleOrderStatusCampaignAsync(order);
    }
}