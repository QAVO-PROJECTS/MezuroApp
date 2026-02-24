using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MezuroApp.Application.Abstracts.Repositories.Orders;
using MezuroApp.Application.Abstracts.Repositories.PaymentTransactions;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Payment;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace MezuroApp.Persistance.Concretes.Services;

public sealed class PaymentService : IPaymentService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _cfg;

    private readonly IOrderReadRepository _orderRead;
    private readonly IOrderWriteRepository _orderWrite;

    private readonly IPaymentTransactionReadRepository _trxRead;
    private readonly IPaymentTransactionWriteRepository _trxWrite;

    public PaymentService(
        IHttpClientFactory factory,
        IConfiguration cfg,
        IOrderReadRepository orderRead,
        IOrderWriteRepository orderWrite,
        IPaymentTransactionReadRepository trxRead,
        IPaymentTransactionWriteRepository trxWrite)
    {
        _http = factory.CreateClient();
        _cfg = cfg;

        _orderRead = orderRead;
        _orderWrite = orderWrite;

        _trxRead = trxRead;
        _trxWrite = trxWrite;
    }

    public async Task<PaymentInitResultDto> StartEpointAsync(
        string? userId,
        StartEpointPaymentDto dto,
        string? ip,
        string? userAgent,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(dto.OrderId, out var oid))
            throw new GlobalAppException("INVALID_ORDER_ID");

        Guid? uid = null;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            if (!Guid.TryParse(userId, out var parsed))
                throw new GlobalAppException("INVALID_USER_ID");
            uid = parsed;
        }

        if (uid == null && string.IsNullOrWhiteSpace(dto.FootprintId))
            throw new GlobalAppException("FOOTPRINT_REQUIRED");

        // ORDER owner-check
        var order = await _orderRead.GetAsync(
            o => !o.IsDeleted
                 && o.Id == oid
                 && (
                     (uid != null && o.UserId == uid) ||
                     (uid == null && o.FootprintId == dto.FootprintId)
                 ),
            enableTracking: true
        );

        if (order == null)
            throw new GlobalAppException("ORDER_NOT_FOUND");

        // artıq paid-dirsə
        var ps = (order.PaymentStatus ?? "").Trim().ToLowerInvariant();
        if (ps == "paid" || ps == "completed")
            throw new GlobalAppException("ORDER_ALREADY_PAID");

        var publicKey = _cfg["Epoint:PublicKey"] ?? throw new Exception("Epoint:PublicKey missing");
        var privateKey = _cfg["Epoint:PrivateKey"] ?? throw new Exception("Epoint:PrivateKey missing");
        var frontend = _cfg["Frontend:BaseUrl"] ?? "";

        // 1) local transaction create (pending)
        var trx = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            PaymentMethod = "epoint",
            Amount = order.Total,
            Currency = "AZN",
            Status = "pending",
            TransactionReference = order.OrderNumber,  // bizim referans
            IpAddress = ip,
            UserAgent = userAgent,
            IsInstallment = dto.IsInstallment,

            InitiatedAt = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            IsDeleted = false
        };

        var db = _trxWrite.GetDbContext();
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        try
        {
            await _trxWrite.AddAsync(trx);
            await _trxWrite.CommitAsync();

            // 2) Epoint request payload
            var jsonObj = new
            {
                public_key = publicKey,
                amount = order.Total,
                currency = "AZN",
                language = "az",
                order_id = order.OrderNumber, // Epoint order_id max 255 (səndə uyğundur)
                description = $"Order {order.OrderNumber}",
                is_installment = dto.IsInstallment ? 1 : 0,
                success_redirect_url = $"{frontend}/payment/success?orderId={order.Id}",
                error_redirect_url = $"{frontend}/payment/failed?orderId={order.Id}"
            };

            var (data, signature) = BuildDataAndSignature(jsonObj, privateKey);

            using var resp = await _http.PostAsync(
                "https://epoint.az/api/1/request",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["data"] = data,
                    ["signature"] = signature
                }),
                ct);

            var body = await resp.Content.ReadAsStringAsync(ct);
            resp.EnsureSuccessStatusCode();

            var ep = JsonSerializer.Deserialize<EpointRequestResponse>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new Exception("EPOINT_RESPONSE_PARSE_ERROR");

            if (!string.Equals(ep.status, "success", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(ep.redirect_url)
                || string.IsNullOrWhiteSpace(ep.transaction))
            {
                trx.Status = "failed";
                trx.ErrorMessage = ep.message ?? "Epoint start failed";
                trx.FailedAt = DateTime.UtcNow;
                trx.GatewayResponse = body;
                trx.LastUpdatedDate = DateTime.UtcNow;

                await _trxWrite.UpdateAsync(trx);
                await _trxWrite.CommitAsync();

                order.PaymentStatus = "failed";
                order.PaymentMethod = "epoint";
                order.LastUpdatedDate = DateTime.UtcNow;
                await _orderWrite.UpdateAsync(order);
                await _orderWrite.CommitAsync();

                await tx.CommitAsync(ct);
                throw new GlobalAppException("PAYMENT_START_FAILED");
            }

            // 3) update trx -> processing
            trx.TransactionId = ep.transaction;
            trx.RedirectUrl = ep.redirect_url;
            trx.GatewayResponse = body;
            trx.Status = "processing";
            trx.LastUpdatedDate = DateTime.UtcNow;

            await _trxWrite.UpdateAsync(trx);
            await _trxWrite.CommitAsync();

            // 4) update order -> processing
            order.PaymentStatus = "processing";
            order.PaymentMethod = "epoint";
            order.LastUpdatedDate = DateTime.UtcNow;

            await _orderWrite.UpdateAsync(order);
            await _orderWrite.CommitAsync();

            await tx.CommitAsync(ct);

            return new PaymentInitResultDto(
                order.Id.ToString(),
                trx.TransactionId!,
                trx.Amount,
                trx.RedirectUrl!
            );
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task HandleEpointCallbackAsync(EpointCallbackDto dto, CancellationToken ct = default)
    {
        var privateKey = _cfg["Epoint:PrivateKey"] ?? throw new Exception("Epoint:PrivateKey missing");

        // 1) signature verify
        var expected = BuildSignature(privateKey, dto.data);
        if (!string.Equals(expected, dto.signature, StringComparison.Ordinal))
            throw new SecurityException("EPOINT_SIGNATURE_MISMATCH");

        // 2) decode
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(dto.data));
        var payload = JsonSerializer.Deserialize<EpointCallbackResult>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new Exception("EPOINT_CALLBACK_PARSE_ERROR");

        // 3) only payment
        if (payload.operation_code != "100")
            return;

        if (string.IsNullOrWhiteSpace(payload.transaction))
            return;

        var trx = await _trxRead.GetAsync(
            t => !t.IsDeleted && t.TransactionId == payload.transaction,
            enableTracking: true
        );

        if (trx == null)
            return; // unknown transaction — log etsən yaxşıdır

        var ok = string.Equals(payload.status, "success", StringComparison.OrdinalIgnoreCase);

        trx.GatewayResponse = json;
        trx.LastUpdatedDate = DateTime.UtcNow;

        if (ok)
        {
            trx.Status = "completed";
            trx.CompletedAt = DateTime.UtcNow;
            trx.FailedAt = null;
        }
        else
        {
            trx.Status = "failed";
            trx.FailedAt = DateTime.UtcNow;
            trx.ErrorCode = payload.code;
            trx.ErrorMessage = payload.message;
        }

        await _trxWrite.UpdateAsync(trx);
        await _trxWrite.CommitAsync();

        // order update
        var order = await _orderWrite.GetDbContext().Set<Order>()
            .FirstOrDefaultAsync(o => !o.IsDeleted && o.Id == trx.OrderId, ct);

        if (order != null)
        {
            order.PaymentStatus = ok ? "paid" : "failed";
            order.PaymentMethod = "epoint";
            order.LastUpdatedDate = DateTime.UtcNow;

            // istəsən burda statusu da dəyiş:
            // if (ok) order.Status = "pending"; // və ya "confirmed" səndə necədirsə

            await _orderWrite.UpdateAsync(order);
            await _orderWrite.CommitAsync();
        }
    }

    public async Task<PaymentStatusDto> GetPaymentStatusAsync(
        string? userId,
        string orderId,
        string? footprintId,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(orderId, out var oid))
            throw new GlobalAppException("INVALID_ORDER_ID");

        Guid? uid = null;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            if (!Guid.TryParse(userId, out var parsed))
                throw new GlobalAppException("INVALID_USER_ID");
            uid = parsed;
        }

        if (uid == null && string.IsNullOrWhiteSpace(footprintId))
            throw new GlobalAppException("FOOTPRINT_REQUIRED");

        // order owner-check
        var order = await _orderRead.GetAsync(
            o => !o.IsDeleted
                 && o.Id == oid
                 && (
                     (uid != null && o.UserId == uid) ||
                     (uid == null && o.FootprintId == footprintId)
                 ),
            include: q => q.Include(x => x.PaymentTransactions!.Where(t => !t.IsDeleted)),
            enableTracking: false
        );

        if (order == null)
            throw new GlobalAppException("ORDER_NOT_FOUND");

        var last = order.PaymentTransactions?
            .OrderByDescending(t => t.InitiatedAt)
            .FirstOrDefault();

        return new PaymentStatusDto(
            order.Id.ToString(),
            (order.PaymentStatus ?? "pending"),
            last?.TransactionId,
            last?.Amount ?? order.Total,
            last?.Currency ?? "AZN"
        );
    }

    // ===== helpers =====

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

    private sealed class EpointRequestResponse
    {
        public string status { get; set; } = default!;
        public string? redirect_url { get; set; }
        public string? transaction { get; set; }
        public string? message { get; set; }
    }

    private sealed class EpointCallbackResult
    {
        public string? order_id { get; set; }
        public string? status { get; set; }
        public string? code { get; set; }
        public string? message { get; set; }
        public string? transaction { get; set; }
        public string? bank_transaction { get; set; }
        public string? bank_response { get; set; }
        public string? operation_code { get; set; }
        public string? rrn { get; set; }
        public string? card_name { get; set; }
        public string? card_mask { get; set; }
        public decimal? amount { get; set; }
    }
}