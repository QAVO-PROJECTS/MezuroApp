using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DnsClient.Protocol;
using Microsoft.EntityFrameworkCore;
using MezuroApp.Application.Abstracts.Repositories.Orders;
using MezuroApp.Application.Abstracts.Repositories.PaymentTransactions;
using MezuroApp.Application.Abstracts.Repositories.UserCards;
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
    private readonly IUserCardReadRepository _userCardRead;
    private readonly IUserCardWriteRepository _userCardWrite;

    public PaymentService(
        IHttpClientFactory factory,
        IConfiguration cfg,
        IOrderReadRepository orderRead,
        IOrderWriteRepository orderWrite,
        IPaymentTransactionReadRepository trxRead,
        IPaymentTransactionWriteRepository trxWrite,
        IUserCardReadRepository userCardRead,
        IUserCardWriteRepository userCardWrite
        )
    {
        _http = factory.CreateClient("epoint");
        _cfg = cfg;

        _orderRead = orderRead;
        _orderWrite = orderWrite;

        _trxRead = trxRead;
        _trxWrite = trxWrite;
        _userCardRead = userCardRead;
        _userCardWrite = userCardWrite;
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

    var ps = (order.PaymentStatus ?? "").Trim().ToLowerInvariant();
    if (ps is "paid" or "completed")
        throw new GlobalAppException("ORDER_ALREADY_PAID");

    if (order.Total <= 0)
        throw new GlobalAppException("INVALID_AMOUNT");

    var publicKey  = _cfg["Epoint:PublicKey"]  ?? throw new Exception("Epoint:PublicKey missing");
    var privateKey = _cfg["Epoint:PrivateKey"] ?? throw new Exception("Epoint:PrivateKey missing");
    var frontend   = _cfg["Frontend:BaseUrl"]  ?? "";
    var backend    = _cfg["Backend:BaseUrl"]   ?? "";

    // ✅ 1) əvvəl local trx yarat: pending
    var trx = new PaymentTransaction
    {
        Id = Guid.NewGuid(),
        OrderId = order.Id,
        PaymentMethod = "epoint",
        Amount = order.Total,
        Currency = "AZN",
        Status = "pending",
        TransactionReference = order.OrderNumber,
        IpAddress = ip,
        UserAgent = userAgent,
        IsInstallment = dto.IsInstallment,
        SaveCardRequested = dto.SaveCard, // ✅ NEW
        InitiatedAt = DateTime.UtcNow,
        CreatedDate = DateTime.UtcNow,
        LastUpdatedDate = DateTime.UtcNow,
        IsDeleted = false
    };

    await _trxWrite.AddAsync(trx);
    await _trxWrite.CommitAsync();

    // ✅ 2) hansı endpoint?
    var endpoint = dto.SaveCard
        ? "https://epoint.az/api/1/card-registration-with-pay"
        : "https://epoint.az/api/1/request";

    // ✅ 3) payload
    // Qeyd: card-registration-with-pay docs-da is_installment göstərilmirsə, problem çıxarsa çıxardarsan.
    var payloadObj = new
    {
        public_key = publicKey,
        language = "az",
        order_id = order.OrderNumber,
        amount = order.Total,
        currency = "AZN",
        description = $"Order {order.OrderNumber}",
        success_redirect_url = $"{frontend}/success?orderId={order.Id}",
        error_redirect_url = $"{frontend}/failed?orderId={order.Id}",
        result_url = $"{backend}/api/payments/epoint/callback",
        is_installment = dto.IsInstallment ? 1 : 0
    };

    var (data, signature) = BuildDataAndSignature(payloadObj, privateKey);
    

    string body;
    try
    {
        using var resp = await _http.PostAsync(
            endpoint,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["data"] = data,
                ["signature"] = signature
            }),
            ct);

        body = await resp.Content.ReadAsStringAsync(ct);
        resp.EnsureSuccessStatusCode();
    }
    catch (Exception ex)
    {
        // ✅ start request fail -> trx failed + order failed
        trx.Status = "failed";
        trx.ErrorMessage = ex.Message;
        trx.FailedAt = DateTime.UtcNow;
        trx.LastUpdatedDate = DateTime.UtcNow;

        await _trxWrite.UpdateAsync(trx);
        await _trxWrite.CommitAsync();

        order.PaymentStatus = "failed";
        order.PaymentMethod = "epoint";
        order.LastUpdatedDate = DateTime.UtcNow;

        await _orderWrite.UpdateAsync(order);
        await _orderWrite.CommitAsync();

        throw new GlobalAppException("PAYMENT_START_FAILED");
    }

    // ✅ 4) parse response (request və card-registration-with-pay çox vaxt oxşar qaytarır: status, redirect_url, transaction)
    EpointRequestResponse ep;
    try
    {
        ep = JsonSerializer.Deserialize<EpointRequestResponse>(
                 body,
                 new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
             ) ?? throw new Exception("EPOINT_RESPONSE_PARSE_ERROR");
    }
    catch
    {
        trx.Status = "failed";
        trx.ErrorMessage = "Epoint response parse failed";
        trx.GatewayResponse = body;
        trx.FailedAt = DateTime.UtcNow;
        trx.LastUpdatedDate = DateTime.UtcNow;

        await _trxWrite.UpdateAsync(trx);
        await _trxWrite.CommitAsync();

        order.PaymentStatus = "failed";
        order.PaymentMethod = "epoint";
        order.LastUpdatedDate = DateTime.UtcNow;

        await _orderWrite.UpdateAsync(order);
        await _orderWrite.CommitAsync();

        throw new GlobalAppException("PAYMENT_START_FAILED");
    }

    var ok = string.Equals(ep.status, "success", StringComparison.OrdinalIgnoreCase)
             && !string.IsNullOrWhiteSpace(ep.redirect_url)
             && !string.IsNullOrWhiteSpace(ep.transaction);

    if (!ok)
    {
        trx.Status = "failed";
        trx.ErrorMessage = ep.message ?? "Epoint start failed";
        trx.GatewayResponse = body;
        trx.FailedAt = DateTime.UtcNow;
        trx.LastUpdatedDate = DateTime.UtcNow;

        await _trxWrite.UpdateAsync(trx);
        await _trxWrite.CommitAsync();

        order.PaymentStatus = "failed";
        order.PaymentMethod = "epoint";
        order.LastUpdatedDate = DateTime.UtcNow;

        await _orderWrite.UpdateAsync(order);
        await _orderWrite.CommitAsync();

        throw new GlobalAppException("PAYMENT_START_FAILED");
    }

    // ✅ 5) burada completed YOX! yalnız processing
    trx.TransactionId = ep.transaction;
    trx.RedirectUrl = ep.redirect_url;
    trx.GatewayResponse = body;
    trx.Status = "processing";
    trx.LastUpdatedDate = DateTime.UtcNow;

    await _trxWrite.UpdateAsync(trx);
    await _trxWrite.CommitAsync();

    order.PaymentStatus = "processing";
    order.PaymentMethod = "epoint";
    order.LastUpdatedDate = DateTime.UtcNow;

    await _orderWrite.UpdateAsync(order);
    await _orderWrite.CommitAsync();

    return new PaymentInitResultDto(
        order.Id.ToString(),
        trx.TransactionId!,
        trx.Amount,
        trx.RedirectUrl!
    );
}



public async Task ReverseEpointAsync(string userId, ReverseEpointDto dto, CancellationToken ct = default)
{
    if (!Guid.TryParse(userId, out var uid))
        throw new GlobalAppException("INVALID_USER_ID");

    if (!Guid.TryParse(dto.OrderId, out var oid))
        throw new GlobalAppException("INVALID_ORDER_ID");

    var order = await _orderRead.GetAsync(
        o => !o.IsDeleted && o.Id == oid && o.UserId == uid,
        enableTracking: true
    );

    if (order == null)
        throw new GlobalAppException("ORDER_NOT_FOUND");

    // ✅ son COMPLETED trx götür
    var trx = await _trxRead.Query()
        .AsTracking()
        .Where(t => !t.IsDeleted
                    && t.OrderId == order.Id
                    && t.Status == "completed")
        .OrderByDescending(t => t.CompletedAt ?? t.LastUpdatedDate)
        .FirstOrDefaultAsync(ct);

    if (trx == null)
        throw new GlobalAppException("TRANSACTION_NOT_FOUND");

    // ✅ artıq reverse/refund olunubsa bir daha etmə
    if (trx.Status is "refunded" or "reversed")
        throw new GlobalAppException("ALREADY_REFUNDED");

    if (string.IsNullOrWhiteSpace(trx.TransactionId))
        throw new GlobalAppException("TRANSACTION_ID_MISSING");

    var publicKey  = _cfg["Epoint:PublicKey"]  ?? throw new Exception("Epoint:PublicKey missing");
    var privateKey = _cfg["Epoint:PrivateKey"] ?? throw new Exception("Epoint:PrivateKey missing");

    // ✅ reverse payload: amount göndərmirik (tam cancel)
    var payloadObj = new
    {
        public_key = publicKey,
        language = "az",
        transaction = trx.TransactionId,
        currency = trx.Currency ?? "AZN"
    };

    var payloadJson = JsonSerializer.Serialize(payloadObj);
    var (data, signature) = BuildDataAndSignature(payloadObj, privateKey);

    Console.WriteLine("========== EPOINT REVERSE DEBUG ==========");
    Console.WriteLine($"OrderId: {order.Id} | OrderNumber: {order.OrderNumber}");
    Console.WriteLine($"Trx(local): {trx.Id} | Trx(gateway): {trx.TransactionId}");
    Console.WriteLine($"Payload JSON: {payloadJson}");
    Console.WriteLine($"DATA(base64): {data}");
    Console.WriteLine($"SIGNATURE: {signature}");
    Console.WriteLine("=========================================");

    using var resp = await _http.PostAsync(
        "https://epoint.az/api/1/reverse",
        new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["data"] = data,
            ["signature"] = signature
        }),
        ct);

    var body = await resp.Content.ReadAsStringAsync(ct);

    Console.WriteLine("========== EPOINT REVERSE RESPONSE ==========");
    Console.WriteLine($"HTTP: {(int)resp.StatusCode} {resp.ReasonPhrase}");
    Console.WriteLine($"RAW BODY: {body}");
    Console.WriteLine("============================================");

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

        // istəyirsənsə xüsusi error:
        // if (ep.message?.Contains("cannot cancel", StringComparison.OrdinalIgnoreCase) == true)
        //     throw new GlobalAppException("REVERSE_NOT_ALLOWED");

        throw new GlobalAppException("REVERSE_FAILED");
    }

    // ✅ local update
    trx.Status = "refunded"; // "refunded" da yaza bilərsən, amma reverse ayrıdır
    trx.GatewayResponse = body;
    trx.LastUpdatedDate = DateTime.UtcNow;
    await _trxWrite.UpdateAsync(trx);
    await _trxWrite.CommitAsync();

    order.PaymentStatus = "refunded";
    order.LastUpdatedDate = DateTime.UtcNow;
    await _orderWrite.UpdateAsync(order);
    await _orderWrite.CommitAsync();
}



private sealed class EpointReverseResponse
{
    public string status { get; set; } = default!;
    public string? message { get; set; }
    public string? code { get; set; }
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
    Console.WriteLine("========== EPOINT CALLBACK RAW ==========");
    Console.WriteLine(json);
    Console.WriteLine("========================================");

    var payload = JsonSerializer.Deserialize<EpointCallbackResult>(
        json,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    ) ?? throw new Exception("EPOINT_CALLBACK_PARSE_ERROR");

    // payment operation codes (səndə 200 gəlib)
    if (payload.operation_code is not ("100" or "200"))
        return;

    // 4) find trx (first by transaction, fallback by order_id)
    PaymentTransaction? trx = null;

    if (!string.IsNullOrWhiteSpace(payload.transaction))
    {
        trx = await _trxRead.GetAsync(
            t => !t.IsDeleted && t.TransactionId == payload.transaction,
            enableTracking: true
        );
    }

    // fallback: order_id -> TransactionReference (səndə order.OrderNumber)
    if (trx == null && !string.IsNullOrWhiteSpace(payload.order_id))
    {
        trx = await _trxRead.Query()
            .AsTracking()
            .Where(t => !t.IsDeleted && t.TransactionReference == payload.order_id)
            .OrderByDescending(t => t.InitiatedAt)
            .FirstOrDefaultAsync(ct);
    }

    if (trx == null)
    {
        Console.WriteLine("⚠️ CALLBACK: Transaction not found (by transaction/order_id).");
        return;
    }

    // 5) update trx status
    var ok = string.Equals(payload.status, "success", StringComparison.OrdinalIgnoreCase);

    trx.GatewayResponse = json;
    trx.LastUpdatedDate = DateTime.UtcNow;

    // card uid resolve (yalnız save üçün lazımdır)
    var cardUid = payload.card_uid ?? payload.card_id;
    if (!string.IsNullOrWhiteSpace(cardUid))
        trx.CardUid = cardUid;

    if (ok)
    {
        trx.Status = "completed";
        trx.CompletedAt = DateTime.UtcNow;
        trx.FailedAt = null;
        trx.ErrorCode = null;
        trx.ErrorMessage = null;
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

    // 6) update order
    var order = await _orderWrite.GetDbContext().Set<Order>()
        .FirstOrDefaultAsync(o => !o.IsDeleted && o.Id == trx.OrderId, ct);

    if (order != null)
    {
        order.PaymentStatus = ok ? "paid" : "failed";
        order.PaymentMethod = "epoint";
        order.LastUpdatedDate = DateTime.UtcNow;

        await _orderWrite.UpdateAsync(order);
        await _orderWrite.CommitAsync();
    }

    // 7) save card only if requested + user exists + cardUid exists
    if (ok
        && trx.SaveCardRequested
        && order?.UserId != null
        && !string.IsNullOrWhiteSpace(cardUid))
    {
        await SaveOrUpdateUserCardAsync(order.UserId.Value, payload, ct);
    }

    Console.WriteLine("✅ CALLBACK DONE: " +
                      $"trx={trx.Id} status={trx.Status} saveCard={trx.SaveCardRequested} cardUid={(cardUid ?? "null")}");
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
public async Task<PaymentInitResultDto> PayWithSavedCardAsync(
    string userId,
    PayWithSavedCardDto dto,
    string? ip,
    string? userAgent,
    CancellationToken ct = default)
{
    if (!Guid.TryParse(userId, out var uid))
        throw new GlobalAppException("INVALID_USER_ID");

    if (!Guid.TryParse(dto.OrderId, out var oid))
        throw new GlobalAppException("INVALID_ORDER_ID");

    var order = await _orderRead.GetAsync(
        o => !o.IsDeleted && o.Id == oid && o.UserId == uid,
        enableTracking: true);

    if (order == null) throw new GlobalAppException("ORDER_NOT_FOUND");

    var ps = (order.PaymentStatus ?? "").Trim().ToLowerInvariant();
    if (ps is "paid" or "completed")
        throw new GlobalAppException("ORDER_ALREADY_PAID");

    var card = await _userCardRead.GetAsync(
        x => !x.IsDeleted && x.Id == dto.UserCardId && x.UserId == uid,
        enableTracking: false);

    if (card == null || string.IsNullOrWhiteSpace(card.CardUid))
        throw new GlobalAppException("CARD_NOT_FOUND");

    // ✅ Taksit istəyirsə - doc-larda execute-pay üçün taksit parametri yoxdur.
    // Ona görə birbaşa 3DS flow-a keçirik.
    if (dto.IsInstallment)
    {
        var startDto = new StartEpointPaymentDto(
            OrderId: order.Id.ToString(),
            IsInstallment: true,
            FootprintId: null,
            SaveCard: false // artıq kart var, yenidən save eləməyə ehtiyac yox
        );

        return await StartEpointAsync(userId, startDto, ip, userAgent, ct);
    }

    // ---- aşağısı səndəki normal saved-card flow-dur (non-installment) ----
    var publicKey = _cfg["Epoint:PublicKey"] ?? throw new Exception("Epoint:PublicKey missing");
    var privateKey = _cfg["Epoint:PrivateKey"] ?? throw new Exception("Epoint:PrivateKey missing");

    var trx = new PaymentTransaction
    {
        Id = Guid.NewGuid(),
        OrderId = order.Id,
        UserCardId = card.Id,
        PaymentMethod = "epoint",
        Amount = order.Total,
        Currency = "AZN",
        Status = "processing",
        TransactionReference = order.OrderNumber,
        IpAddress = ip,
        UserAgent = userAgent,
        IsInstallment = false,
        InitiatedAt = DateTime.UtcNow,
        CreatedDate = DateTime.UtcNow,
        LastUpdatedDate = DateTime.UtcNow,
        IsDeleted = false
    };
    await _trxWrite.AddAsync(trx);
    await _trxWrite.CommitAsync();

    var payloadObj = new
    {
        public_key = publicKey,
        language = "az",
        card_id = card.CardUid,
        order_id = order.OrderNumber,
        amount = order.Total,
        currency = "AZN",
        description = $"Order {order.OrderNumber}"
    };

    var (data, signature) = BuildDataAndSignature(payloadObj, privateKey);

    using var resp = await _http.PostAsync(
        "https://epoint.az/api/1/execute-pay",
        new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["data"] = data,
            ["signature"] = signature
        }), ct);

    var body = await resp.Content.ReadAsStringAsync(ct);

    var ep = JsonSerializer.Deserialize<EpointExecutePayResponse>(
        body,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    ) ?? throw new Exception("EPOINT_RESPONSE_PARSE_ERROR");

    trx.GatewayResponse = body;
    trx.LastUpdatedDate = DateTime.UtcNow;

    var ok = string.Equals(ep.status, "success", StringComparison.OrdinalIgnoreCase);
    if (ok)
    {
        trx.Status = "completed";
        trx.CompletedAt = DateTime.UtcNow;
        trx.TransactionId = ep.transaction;

        order.PaymentStatus = "paid";
        order.PaymentMethod = "epoint";
        order.LastUpdatedDate = DateTime.UtcNow;

        await _trxWrite.UpdateAsync(trx);
        await _trxWrite.CommitAsync();

        await _orderWrite.UpdateAsync(order);
        await _orderWrite.CommitAsync();

        return new PaymentInitResultDto(order.Id.ToString(), trx.TransactionId ?? "", trx.Amount, "");
    }

    trx.Status = "failed";
    trx.FailedAt = DateTime.UtcNow;
    trx.ErrorCode = ep.code;
    trx.ErrorMessage = ep.message ?? "Execute-pay failed";
    await _trxWrite.UpdateAsync(trx);
    await _trxWrite.CommitAsync();

    throw new GlobalAppException("PAYMENT_FAILED");
}

public async Task AdminReverseEpointAsync(AdminReverseEpointDto dto, CancellationToken ct = default)
{
    if (!Guid.TryParse(dto.OrderId, out var oid))
        throw new GlobalAppException("INVALID_ORDER_ID");

    var order = await _orderRead.GetAsync(
        o => !o.IsDeleted && o.Id == oid,
        enableTracking: true
    );

    if (order == null)
        throw new GlobalAppException("ORDER_NOT_FOUND");

    // ✅ son COMPLETED trx götür
    var trx = await _trxRead.Query()
        .AsTracking()
        .Where(t => !t.IsDeleted
                    && t.OrderId == order.Id
                    && t.Status == "completed")
        .OrderByDescending(t => t.CompletedAt ?? t.LastUpdatedDate)
        .FirstOrDefaultAsync(ct);

    if (trx == null)
        throw new GlobalAppException("TRANSACTION_NOT_FOUND");

    // ✅ artıq reverse/refund olunubsa bir daha etmə
    if (trx.Status is "refunded" or "reversed")
        throw new GlobalAppException("ALREADY_REFUNDED");

    if (string.IsNullOrWhiteSpace(trx.TransactionId))
        throw new GlobalAppException("TRANSACTION_ID_MISSING");

    var publicKey  = _cfg["Epoint:PublicKey"]  ?? throw new Exception("Epoint:PublicKey missing");
    var privateKey = _cfg["Epoint:PrivateKey"] ?? throw new Exception("Epoint:PrivateKey missing");

    // ✅ reverse payload: amount göndərmirik (tam cancel)
    var payloadObj = new
    {
        public_key = publicKey,
        language = "az",
        transaction = trx.TransactionId,
        currency = trx.Currency ?? "AZN"
    };

    var payloadJson = JsonSerializer.Serialize(payloadObj);
    var (data, signature) = BuildDataAndSignature(payloadObj, privateKey);

    Console.WriteLine("========== EPOINT ADMIN REVERSE DEBUG ==========");
    Console.WriteLine($"OrderId: {order.Id} | OrderNumber: {order.OrderNumber}");
    Console.WriteLine($"Trx(local): {trx.Id} | Trx(gateway): {trx.TransactionId}");
    Console.WriteLine($"Payload JSON: {payloadJson}");
    Console.WriteLine($"DATA(base64): {data}");
    Console.WriteLine($"SIGNATURE: {signature}");
    Console.WriteLine("===============================================");

    using var resp = await _http.PostAsync(
        "https://epoint.az/api/1/reverse",
        new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["data"] = data,
            ["signature"] = signature
        }),
        ct);

    var body = await resp.Content.ReadAsStringAsync(ct);

    Console.WriteLine("========== EPOINT ADMIN REVERSE RESPONSE ==========");
    Console.WriteLine($"HTTP: {(int)resp.StatusCode} {resp.ReasonPhrase}");
    Console.WriteLine($"RAW BODY: {body}");
    Console.WriteLine("==================================================");

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
    trx.Status = "refunded"; // reverse ayrı olsa da refunds page üçün uyğundur
    trx.RefundedAmount = trx.Amount; // ✅ refunds list-də görünsün (RefundedAmount > 0)
    trx.GatewayResponse = body;
    trx.LastUpdatedDate = DateTime.UtcNow;

    await _trxWrite.UpdateAsync(trx);
    await _trxWrite.CommitAsync();

    // order update
    order.PaymentStatus = "refunded";
    order.LastUpdatedDate = DateTime.UtcNow;

    // Admin note istəyirsənsə Order-a yaz (səndə AdminNote var)
    if (!string.IsNullOrWhiteSpace(dto.Note))
        order.AdminNote = dto.Note;

    await _orderWrite.UpdateAsync(order);
    await _orderWrite.CommitAsync();
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
    private async Task SaveOrUpdateUserCardAsync(
    Guid userId,
    EpointCallbackResult payload,
    CancellationToken ct)
{
    var cardUid = payload.card_uid ?? payload.card_id;

    if (string.IsNullOrWhiteSpace(cardUid))
        return;

    // 1️⃣ Mövcud kart varmı?
    var existing = await _userCardRead.GetAsync(
        x => !x.IsDeleted
            && x.UserId == userId
             && x.CardUid == cardUid,
        enableTracking: true
    );

    if (existing != null)
    {
        // 2️⃣ Mövcud kartı yenilə
        existing.CardName = payload.card_name ?? existing.CardName;
        existing.CardMask = payload.card_mask ?? existing.CardMask;
        existing.CardExpiry = payload.card_expiry ?? existing.CardExpiry;
        existing.BankTransaction = payload.bank_transaction ?? existing.BankTransaction;
        existing.BankResponse = payload.bank_response ?? existing.BankResponse;
        existing.OperationCode = payload.operation_code ?? existing.OperationCode;
        existing.Rrn = payload.rrn ?? existing.Rrn;
        existing.LastUpdatedDate = DateTime.UtcNow;

        await _userCardWrite.UpdateAsync(existing);
        await _userCardWrite.CommitAsync();
        return;
    }

    // 3️⃣ User-in başqa kartı varmı? (default təyini üçün)
    var userCards = await _userCardRead.GetAllAsync(
        x => !x.IsDeleted && x.UserId == userId,
        enableTracking: false
    );

    var hasAnyCard = userCards.Any();

    var newCard = new UserCard
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        CardUid = cardUid,
        CardName = payload.card_name,
        CardMask = payload.card_mask,
        CardExpiry = payload.card_expiry,
        BankTransaction = payload.bank_transaction,
        BankResponse = payload.bank_response,
        OperationCode = payload.operation_code,
        Rrn = payload.rrn,
        IsDefault = !hasAnyCard, // ilk kartdırsa default olsun
        CreatedDate = DateTime.UtcNow,
        LastUpdatedDate = DateTime.UtcNow,
        IsDeleted = false
    };

    await _userCardWrite.AddAsync(newCard);
    await _userCardWrite.CommitAsync();
}
    private sealed class EpointRefundResponse
    {
        public string status { get; set; } = default!;
        public string? code { get; set; }
        public string? message { get; set; }
    }
    private sealed class EpointRequestResponse
    {
        public string status { get; set; } = default!;
        public string? redirect_url { get; set; }
        public string? transaction { get; set; }
        public string? message { get; set; }
    }

    private sealed class EpointExecutePayResponse
    {
        public string status { get; set; } = default!;
        public string? code { get; set; }
        public string? message { get; set; }
        public string? transaction { get; set; }
        public string? bank_transaction { get; set; }
    }
    private sealed class EpointCallbackResult
    {
        public string? order_id { get; set; }
        public string? status { get; set; }
        public string? code { get; set; }
        public string? message { get; set; }
        public string? card_id { get; set; } // bəzən card_uid kimi də gələ bilər
        public string? card_uid { get; set; }
        public string? transaction { get; set; }
        public string? bank_transaction { get; set; }
        public string? bank_response { get; set; }
        public string? operation_code { get; set; }
        public string? rrn { get; set; }
        public string? card_name { get; set; }
        public string? card_mask { get; set; }
        public string? card_expiry { get; set; }
        public decimal? amount { get; set; }
    }
}