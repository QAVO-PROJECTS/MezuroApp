using Microsoft.EntityFrameworkCore;
using MezuroApp.Application.Abstracts.Repositories.Orders;
using MezuroApp.Application.Abstracts.Repositories.PaymentTransactions;
using MezuroApp.Application.Abstracts.Services;

using MezuroApp.Application.Dtos.Order.AdminOrder; // sən öz namespace-ni qoy
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;
using MezuroApp.Domain.HelperEntities;

public sealed class AdminRefundService : IAdminRefundService
{
    private readonly IPaymentTransactionReadRepository _trxRead;
    private readonly IOrderReadRepository _orderRead; // detail üçün lazım ola bilər

    public AdminRefundService(
        IPaymentTransactionReadRepository trxRead,
        IOrderReadRepository orderRead)
    {
        _trxRead = trxRead;
        _orderRead = orderRead;
    }

    public async Task<PagedResult<AdminRefundListItemDto>> GetRefundsAsync(AdminRefundListFilterDto f, CancellationToken ct)
    {
        IQueryable<PaymentTransaction> q = _trxRead.Query()
            .AsNoTracking()
            .Where(t => !t.IsDeleted && t.RefundedAmount > 0m)
            .Include(t => t.Order);

        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var s = f.Search.Trim().ToLowerInvariant();
            q = q.Where(t => t.Order != null && t.Order.OrderNumber.ToLower().Contains(s));
        }

        if (f.FromUtc.HasValue) q = q.Where(t => t.LastUpdatedDate >= f.FromUtc.Value);
        if (f.ToUtc.HasValue) q = q.Where(t => t.LastUpdatedDate <= f.ToUtc.Value);

        if (!string.IsNullOrWhiteSpace(f.Status))
        {
            var st = f.Status.Trim().ToLowerInvariant();
            if (st == "refunded")
                q = q.Where(t => t.RefundedAmount >= t.Amount);
            else if (st is "partial_refunded" or "partial")
                q = q.Where(t => t.RefundedAmount > 0m && t.RefundedAmount < t.Amount);
        }

        var total = await q.CountAsync(ct);

        var page = Math.Max(1, f.Page);
        var size = Math.Clamp(f.PageSize, 1, 200);

        var items = await q
            .OrderByDescending(t => t.LastUpdatedDate)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(t => new AdminRefundListItemDto(
                t.Id,
                t.OrderId,
                t.Order != null ? t.Order.OrderNumber : "",
                t.Amount,
                t.RefundedAmount,
                t.Currency,
                (t.RefundedAmount >= t.Amount) ? "refunded" : "partial_refunded",
                t.LastUpdatedDate
            ))
            .ToListAsync(ct);

        return new PagedResult<AdminRefundListItemDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = size
        };
    }

    public async Task<AdminRefundDetailDto> GetRefundDetailAsync(string paymentTransactionId, CancellationToken ct)
    {
        if (!Guid.TryParse(paymentTransactionId, out var tid))
            throw new GlobalAppException("INVALID_TRANSACTION_ID");

        var trx = await _trxRead.Query()
            .AsNoTracking()
            .Include(t => t.Order)
            .FirstOrDefaultAsync(t => !t.IsDeleted && t.Id == tid, ct);

        if (trx == null)
            throw new GlobalAppException("TRANSACTION_NOT_FOUND");

        if (trx.RefundedAmount <= 0m)
            throw new GlobalAppException("REFUND_NOT_FOUND");

        return new AdminRefundDetailDto(
            PaymentTransactionId: trx.Id,
            OrderId: trx.OrderId,
            OrderNumber: trx.Order?.OrderNumber ?? "",
            PaidAmount: trx.Amount,
            RefundedAmount: trx.RefundedAmount,
            Currency: trx.Currency,
            RefundStatus: (trx.RefundedAmount >= trx.Amount) ? "refunded" : "partial_refunded",
            PaymentMethod: trx.PaymentMethod,
            TransactionId: trx.TransactionId,
            GatewayResponse: trx.GatewayResponse,
            ErrorCode: trx.ErrorCode,
            ErrorMessage: trx.ErrorMessage,
            InitiatedAt: trx.InitiatedAt,
            CompletedAt: trx.CompletedAt,
            LastUpdatedDate: trx.LastUpdatedDate
        );
    }
}