using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MezuroApp.Application.Abstracts.Repositories.Orders;
using MezuroApp.Application.Abstracts.Repositories.PaymentTransactions;
using MezuroApp.Application.Abstracts.Services;

using MezuroApp.Application.Dtos.Transaction;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;
using MezuroApp.Domain.HelperEntities;

namespace MezuroApp.Persistance.Concretes.Services;

public sealed class AdminTransactionService : IAdminTransactionService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _cfg;

    private readonly IPaymentTransactionReadRepository _trxRead;
    private readonly IPaymentTransactionWriteRepository _trxWrite;

    private readonly IOrderReadRepository _orderRead;
    private readonly IOrderWriteRepository _orderWrite;

    public AdminTransactionService(
        IHttpClientFactory factory,
        IConfiguration cfg,
        IPaymentTransactionReadRepository trxRead,
        IPaymentTransactionWriteRepository trxWrite,
        IOrderReadRepository orderRead,
        IOrderWriteRepository orderWrite)
    {
        _http = factory.CreateClient("epoint");
        _cfg = cfg;

        _trxRead = trxRead;
        _trxWrite = trxWrite;
        _orderRead = orderRead;
        _orderWrite = orderWrite;
    }

    private static bool AllFiltersEmpty(AdminTransactionListFilterDto f)
    {
        return string.IsNullOrWhiteSpace(f.Search)
               && string.IsNullOrWhiteSpace(f.PaymentMethod)
               && string.IsNullOrWhiteSpace(f.Status)
               && string.IsNullOrWhiteSpace(f.From)
               && string.IsNullOrWhiteSpace(f.To)
               && !f.MinAmount.HasValue
               && !f.MaxAmount.HasValue;
    }
     public async Task<AdminTransactionDashboardDto> GetDashboardAsync(AdminTransactionListFilterDto f, CancellationToken ct)
    {
        // base query (filters)
        var baseQ = _trxRead.Query()
            .AsNoTracking()
            .Where(t => !t.IsDeleted)
            .Join(
                _orderRead.Query().AsNoTracking().Where(o => !o.IsDeleted),
                t => t.OrderId,
                o => o.Id,
                (t, o) => new { t, o }
            );

        // Search
        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var s = f.Search.Trim().ToLowerInvariant();
            baseQ = baseQ.Where(x =>
                (x.o.OrderNumber ?? "").ToLower().Contains(s) ||
                (x.o.Email ?? "").ToLower().Contains(s) ||
                ((x.o.Phone ?? "").ToLower().Contains(s)));
        }

        // PaymentMethod
        if (!string.IsNullOrWhiteSpace(f.PaymentMethod))
        {
            var pm = f.PaymentMethod.Trim().ToLowerInvariant();
            baseQ = baseQ.Where(x => (x.t.PaymentMethod ?? "").ToLower() == pm);
        }

        // Status
        if (!string.IsNullOrWhiteSpace(f.Status))
        {
            var st = f.Status.Trim().ToLowerInvariant();
            baseQ = baseQ.Where(x => (x.t.Status ?? "").ToLower() == st);
        }

        // Amount range
        if (f.MinAmount.HasValue) baseQ = baseQ.Where(x => x.t.Amount >= f.MinAmount.Value);
        if (f.MaxAmount.HasValue) baseQ = baseQ.Where(x => x.t.Amount <= f.MaxAmount.Value);

        // Date range (dd.MM.yyyy -> UTC)
        var q = baseQ;

        var allEmpty =
            string.IsNullOrWhiteSpace(f.Search) &&
            string.IsNullOrWhiteSpace(f.PaymentMethod) &&
            string.IsNullOrWhiteSpace(f.Status) &&
            string.IsNullOrWhiteSpace(f.From) &&
            string.IsNullOrWhiteSpace(f.To) &&
            !f.MinAmount.HasValue &&
            !f.MaxAmount.HasValue;

// ✅ yalnız From/To gələndə date tətbiq et
        if (!allEmpty && (!string.IsNullOrWhiteSpace(f.From) || !string.IsNullOrWhiteSpace(f.To)))
        {
            var (fromUtc, toExUtc) = ResolveWindowUtc(f.From, f.To);
            q = q.Where(x => x.t.InitiatedAt >= fromUtc && x.t.InitiatedAt < toExUtc);
        } 

        var kpi = await q
            .GroupBy(_ => 1)
            .Select(g => new                                 
            {
                TotalTransactions = g.Count(),

                TotalRevenue = g.Sum(x =>
                    x.t.Status != null &&
                    (x.t.Status.ToLower() == "completed" || x.t.Status.ToLower() == "paid")
                        ? x.t.Amount
                        : 0m),

                PendingPayments = g.Count(x =>
                    x.t.Status != null &&
                    (x.t.Status.ToLower() == "pending" || x.t.Status.ToLower() == "processing")),

                RefundedAmount = g.Sum(x =>
                  
                    (x.t.Status.ToLower() == "refunded")? x.t.Amount : 0m)
            })
            .FirstOrDefaultAsync(ct);

        var now = DateTime.UtcNow;

// Current month
        var curFrom = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var curToEx = DateTime.SpecifyKind(now.Date.AddDays(1), DateTimeKind.Utc);

// Previous month
        var prevMonth = now.AddMonths(-1);
        var prevFrom = new DateTime(prevMonth.Year, prevMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);

// eyni qədər gün (month-to-date logic)
        var elapsed = curToEx - curFrom;
        var prevToEx = prevFrom + elapsed;
        var curQ = baseQ.Where(x => x.t.InitiatedAt >= curFrom && x.t.InitiatedAt < curToEx);

        var curTotal = await curQ.CountAsync(ct);

        var curRevenue = await curQ
            .Where(x => x.t.Status == "completed" || x.t.Status == "paid")
            .SumAsync(x => (decimal?)x.t.Amount) ?? 0m;

        var curPending = await curQ
            .Where(x => x.t.Status == "pending" || x.t.Status == "processing")
            .CountAsync(ct);

        var curRefunded = await curQ
            .SumAsync(x => (decimal?)x.t.RefundedAmount) ?? 0m;
        var prevQ = baseQ.Where(x => x.t.InitiatedAt >= prevFrom && x.t.InitiatedAt < prevToEx);

        var prevTotal = await prevQ.CountAsync(ct);

        var prevRevenue = await prevQ
            .Where(x => x.t.Status == "completed" || x.t.Status == "paid")
            .SumAsync(x => (decimal?)x.t.Amount) ?? 0m;

        var prevPending = await prevQ
            .Where(x => x.t.Status == "pending" || x.t.Status == "processing")
            .CountAsync(ct);

        var prevRefunded = await prevQ
            .SumAsync(x => x.t.Status=="refunded" ? x.t.Amount : 0m);
        // Səndə DTO change percent field-ləri var — istəmirsənsə 0 qaytarırıq
        return new AdminTransactionDashboardDto(
            TotalTransactions: kpi?.TotalTransactions ?? 0,
            TotalRevenue: kpi?.TotalRevenue ?? 0m,
            PendingPayments: kpi?.PendingPayments ?? 0,
            RefundedAmount: kpi?.RefundedAmount ?? 0m,

            TotalTransactionsChangePercent: CalcChangePercent(curTotal,prevTotal),
            RevenueChangePercent: CalcChangePercent(curRevenue, prevRevenue),
            PendingChangePercent: CalcChangePercent(curPending, prevPending),
            RefundedChangePercent: CalcChangePercent(curRefunded, prevRefunded)
        );
    }
    private static decimal CalcChangePercent(decimal current, decimal previous)
    {
        if (previous == 0m)
        {
            if (current == 0m) return 0m;
            return 100m; // əvvəl 0 idisə və indi artım varsa
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
    // =========================
    // Date helpers (UTC, exclusive end)
    // =========================
    private static (DateTime fromUtc, DateTime toExUtc) ResolveWindowUtc(string? from, string? to)
    {
        // Default: month-to-date
        if (string.IsNullOrWhiteSpace(from) && string.IsNullOrWhiteSpace(to))
        {
            var now = DateTime.UtcNow;
            var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endEx = DateTime.SpecifyKind(now.Date.AddDays(1), DateTimeKind.Utc);
            return (start, endEx);
        }

        var fromUtc = !string.IsNullOrWhiteSpace(from)
            ? ParseDdMmYyyyUtcOrThrow(from!, "INVALID_FROM_DATE")
            : DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

        var toExUtc = !string.IsNullOrWhiteSpace(to)
            ? ParseDdMmYyyyUtcOrThrow(to!, "INVALID_TO_DATE").AddDays(1)
            : DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(1), DateTimeKind.Utc);

        if (toExUtc <= fromUtc)
            throw new GlobalAppException("INVALID_DATE_RANGE");

        return (fromUtc, toExUtc);
    }


    public async Task<PagedResult<AdminTransactionListItemDto>> GetTransactionsAsync(AdminTransactionListFilterDto f, CancellationToken ct)
    {
        // PaymentTransactions JOIN Orders
        var q = _trxRead.Query()
            .AsNoTracking()
            .Where(t => !t.IsDeleted)
            .Join(
                _orderRead.Query().AsNoTracking().Where(o => !o.IsDeleted),
                t => t.OrderId,
                o => o.Id,
                (t, o) => new { t, o }
            );

        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var s = f.Search.Trim().ToLowerInvariant();
            q = q.Where(x =>
                x.o.OrderNumber.ToLower().Contains(s) ||
                x.o.Email.ToLower().Contains(s) ||
                (x.o.Phone != null && x.o.Phone.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(f.PaymentMethod))
        {
            var pm = f.PaymentMethod.Trim().ToLowerInvariant();
            q = q.Where(x => (x.t.PaymentMethod ?? "").ToLower() == pm);
        }

        if (!string.IsNullOrWhiteSpace(f.Status))
        {
            var st = f.Status.Trim().ToLowerInvariant();
            q = q.Where(x => (x.t.Status ?? "").ToLower() == st);
        }

        // dd.MM.yyyy parse (UTC) — səndə Npgsql "timestamp with time zone" var
        if (!string.IsNullOrWhiteSpace(f.From))
        {
            var fromUtc = ParseDdMmYyyyUtcOrThrow(f.From, "INVALID_FROM_DATE");
            q = q.Where(x => x.t.InitiatedAt >= fromUtc);
        }

        if (!string.IsNullOrWhiteSpace(f.To))
        {
            var toUtc = ParseDdMmYyyyUtcOrThrow(f.To, "INVALID_TO_DATE").AddDays(1); // exclusive
            q = q.Where(x => x.t.InitiatedAt < toUtc);
        }

        if (f.MinAmount.HasValue) q = q.Where(x => x.t.Amount >= f.MinAmount.Value);
        if (f.MaxAmount.HasValue) q = q.Where(x => x.t.Amount <= f.MaxAmount.Value);

        var total = await q.CountAsync(ct);

        var page = Math.Max(1, f.Page);
        var size = Math.Clamp(f.PageSize, 1, 200);

        // əvvəl raw alaq (EF ToString format vermir)
        var raw = await q
            .OrderByDescending(x => x.t.InitiatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(x => new
            {
                x.t.Id,
                x.t.OrderId,
                x.o.OrderNumber,
                x.o.FirstName,
                x.o.LastName,
                x.t.PaymentMethod,
                x.t.Status,
                x.t.Amount,
                x.t.RefundedAmount,
                x.t.Currency,
                x.t.InitiatedAt
            })
            .ToListAsync(ct);

        var items = raw.Select(x => new AdminTransactionListItemDto(
            PaymentTransactionId: x.Id.ToString(),
            OrderId: x.OrderId.ToString(),
            OrderNumber: x.OrderNumber,
            CustomerName: (((x.FirstName ?? "") + " " + (x.LastName ?? "")).Trim()),
            PaymentMethod: x.PaymentMethod ?? "unknown",
            Status: x.Status ?? "pending",
            Amount: x.Amount,
            RefundedAmount: x.RefundedAmount,
            Currency: x.Currency ?? "AZN",
            Date: x.InitiatedAt.ToString("dd.MM.yyyy")
        )).ToList();

        return new PagedResult<AdminTransactionListItemDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = size
        };
    }

    public async Task<AdminTransactionDetailDto> GetTransactionDetailAsync(string paymentTransactionId, CancellationToken ct)
    {
        if (!Guid.TryParse(paymentTransactionId, out var tid))
            throw new GlobalAppException("INVALID_TRANSACTION_ID");

        var x = await _trxRead.Query()
            .AsNoTracking()
            .Where(t => !t.IsDeleted && t.Id == tid)
            .Join(
                _orderRead.Query().AsNoTracking().Where(o => !o.IsDeleted),
                t => t.OrderId,
                o => o.Id,
                (t, o) => new { t, o }
            )
            .FirstOrDefaultAsync(ct);

        if (x == null) throw new GlobalAppException("TRANSACTION_NOT_FOUND");

        return new AdminTransactionDetailDto(
            PaymentTransactionId: x.t.Id.ToString(),
            OrderId: x.o.Id.ToString(),
            OrderNumber: x.o.OrderNumber,
            CustomerName: (((x.o.FirstName ?? "") + " " + (x.o.LastName ?? "")).Trim()),
            CustomerEmail: x.o.Email,
            CustomerPhone: x.o.Phone,

            PaymentMethod: x.t.PaymentMethod ?? "unknown",
            Status: x.t.Status ?? "pending",
            Amount: x.t.Amount,
            RefundedAmount: x.t.RefundedAmount,
            Currency: x.t.Currency ?? "AZN",

            InitiatedAt: x.t.InitiatedAt.ToString("dd.MM.yyyy HH:mm"),
            CompletedAt: x.t.CompletedAt.HasValue ? x.t.CompletedAt.Value.ToString("dd.MM.yyyy HH:mm") : null,
            TransactionId: x.t.TransactionId,
            GatewayResponse: x.t.GatewayResponse,
            ErrorCode: x.t.ErrorCode,
            ErrorMessage: x.t.ErrorMessage
        );
    }

    // ✅ Admin Create Refund = Epoint reverse (tam refund)
    public async Task<AdminRefundResultDto> AdminReverseEpointAsync(AdminCreateRefundDto dto, CancellationToken ct)
    {
        if (!Guid.TryParse(dto.PaymentTransactionId, out var tid))
            throw new GlobalAppException("INVALID_TRANSACTION_ID");

        // track lazım olacaq
        var trx = await _trxRead.GetAsync(
            t => !t.IsDeleted && t.Id == tid,
            enableTracking: true);

        if (trx == null) throw new GlobalAppException("TRANSACTION_NOT_FOUND");

        if (!string.Equals(trx.PaymentMethod, "epoint", StringComparison.OrdinalIgnoreCase))
            throw new GlobalAppException("REFUND_ONLY_EPOINT");

        // yalnız completed transaction reverse olunsun
        if (!string.Equals(trx.Status, "completed", StringComparison.OrdinalIgnoreCase))
            throw new GlobalAppException("REFUND_ONLY_COMPLETED");

        if (trx.RefundedAmount >= trx.Amount)
            throw new GlobalAppException("ALREADY_REFUNDED");

        if (string.IsNullOrWhiteSpace(trx.TransactionId))
            throw new GlobalAppException("TRANSACTION_ID_MISSING");

        var publicKey = _cfg["Epoint:PublicKey"] ?? throw new Exception("Epoint:PublicKey missing");
        var privateKey = _cfg["Epoint:PrivateKey"] ?? throw new Exception("Epoint:PrivateKey missing");

        var payloadObj = new
        {
            public_key = publicKey,
            language = "az",
            transaction = trx.TransactionId,
            currency = trx.Currency ?? "AZN"
        };

        var (data, signature) = BuildDataAndSignature(payloadObj, privateKey);

        using var resp = await _http.PostAsync(
            "https://epoint.az/api/1/reverse",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["data"] = data,
                ["signature"] = signature
            }),
            ct);

        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            trx.ErrorMessage = $"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}";
            trx.GatewayResponse = body;
            trx.LastUpdatedDate = DateTime.UtcNow;
            await _trxWrite.UpdateAsync(trx);
            await _trxWrite.CommitAsync();
            throw new GlobalAppException("REVERSE_FAILED");
        }

        var ep = JsonSerializer.Deserialize<EpointReverseResponse>(
            body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        ) ?? throw new Exception("EPOINT_RESPONSE_PARSE_ERROR");

        var ok = string.Equals(ep.status, "success", StringComparison.OrdinalIgnoreCase);
        if (!ok)
        {
            trx.ErrorMessage = ep.message ?? "Reverse failed";
            trx.GatewayResponse = body;
            trx.LastUpdatedDate = DateTime.UtcNow;
            await _trxWrite.UpdateAsync(trx);
            await _trxWrite.CommitAsync();
            throw new GlobalAppException("REVERSE_FAILED");
        }

        // ✅ local update
        trx.RefundedAmount = trx.Amount; // full refund
        trx.Status = "refunded";
        trx.GatewayResponse = body;
        trx.LastUpdatedDate = DateTime.UtcNow;

        await _trxWrite.UpdateAsync(trx);
        await _trxWrite.CommitAsync();

        // order status update
        var order = await _orderRead.GetAsync(o => !o.IsDeleted && o.Id == trx.OrderId, enableTracking: true);
        if (order != null)
        {
            order.PaymentStatus = "refunded";
            order.PaymentMethod = "epoint";
            order.LastUpdatedDate = DateTime.UtcNow;
            await _orderWrite.UpdateAsync(order);
            await _orderWrite.CommitAsync();
        }

        return new AdminRefundResultDto(
            PaymentTransactionId: trx.Id.ToString(),
            RefundStatus: "refunded",
            RefundedAmount: trx.RefundedAmount,
            Currency: trx.Currency ?? "AZN"
        );
    }

    private sealed class EpointReverseResponse
    {
        public string status { get; set; } = default!;
        public string? message { get; set; }
        public string? code { get; set; }
    }

    private static (string data, string signature) BuildDataAndSignature(object jsonObj, string privateKey)
    {
        var json = JsonSerializer.Serialize(jsonObj);
        var data = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        var signature = BuildSignature(privateKey, data);
        return (data, signature);
    }

    private static string BuildSignature(string privateKey, string data)
    {
        using var sha = SHA1.Create();
        var raw = $"{privateKey}{data}{privateKey}";
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToBase64String(hash);
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

        // Npgsql timestamp with time zone -> UTC olmalıdır
        return DateTime.SpecifyKind(dt.Date, DateTimeKind.Utc);
    }
}