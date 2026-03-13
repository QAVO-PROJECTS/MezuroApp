using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MezuroApp.Application.Abstracts.Repositories.Baskets;
using MezuroApp.Application.Abstracts.Repositories.Cupons;
using MezuroApp.Application.Abstracts.Repositories.Orders;
using MezuroApp.Application.Abstracts.Repositories.Products;
using MezuroApp.Application.Abstracts.Repositories.ProductVariants;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Order;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;

namespace MezuroApp.Persistance.Concretes.Services;

public class OrderService : IOrderService
    
{
    private readonly IBasketReadRepository _basketRead;
    private readonly IBasketWriteRepository _basketWrite;

    private readonly IOrderReadRepository _orderRead;
    private readonly IOrderWriteRepository _orderWrite;

    private readonly IProductReadRepository _productRead;
    private readonly IProductWriteRepository _productWrite;

    private readonly IProductVariantReadRepository _pvRead;
    private readonly IProductVariantWriteRepository _pvWrite;

    private readonly ICuponReadRepository _cuponRead;
    private readonly ICuponWriteRepository _cuponWrite;

    private readonly UserManager<User> _userManager;
    private readonly IMapper _mapper;
    private readonly IEmailCampaignService _campaignService;

    public OrderService(
        IBasketReadRepository basketRead,
        IBasketWriteRepository basketWrite,
        IOrderReadRepository orderRead,
        IOrderWriteRepository orderWrite,
        IProductReadRepository productRead,
        IProductWriteRepository productWrite,
        IProductVariantReadRepository pvRead,
        IProductVariantWriteRepository pvWrite,
        ICuponReadRepository cuponRead,
        ICuponWriteRepository cuponWrite,
        UserManager<User> userManager,
        IMapper mapper,
        IEmailCampaignService campaignService)
    {
        _basketRead = basketRead;
        _basketWrite = basketWrite;

        _orderRead = orderRead;
        _orderWrite = orderWrite;

        _productRead = productRead;
        _productWrite = productWrite;

        _pvRead = pvRead;
        _pvWrite = pvWrite;

        _cuponRead = cuponRead;
        _cuponWrite = cuponWrite;

        _userManager = userManager;
        _mapper = mapper;
        _campaignService = campaignService;
    }

    // ==========================================================
    // CHECKOUT -> CREATE ORDER
    // ==========================================================
public async Task<OrderCreatedDto> CreateFromCheckoutAsync(string? userId, CreateOrderCheckoutDto dto)
{
    // 1) Identify user / guest
    Guid? uid = null;

    if (!string.IsNullOrWhiteSpace(userId))
    {
        if (!Guid.TryParse(userId, out var parsed))
            throw new GlobalAppException("INVALID_USER_ID");

        uid = parsed;
    }

    if (uid == null && string.IsNullOrWhiteSpace(dto.FootprintId))
        throw new GlobalAppException("INVALID_FOOTPRINT");

    // 2) Resolve user fields
    User? user = null;
    if (uid != null)
    {
        user = await _userManager.FindByIdAsync(uid.Value.ToString());
        if (user == null || user.IsDeleted)
            throw new GlobalAppException("USER_NOT_FOUND");
    }

    var resolvedEmail = uid != null
        ? (string.IsNullOrWhiteSpace(dto.Email) ? user!.Email : dto.Email)
        : dto.Email;

    if (string.IsNullOrWhiteSpace(resolvedEmail))
        throw new GlobalAppException("EMAIL_REQUIRED");

    var resolvedPhone = string.IsNullOrWhiteSpace(dto.Phone) ? user?.PhoneNumber : dto.Phone;
    var resolvedFirstName = string.IsNullOrWhiteSpace(dto.FirstName) ? user?.FirstName : dto.FirstName;
    var resolvedLastName = string.IsNullOrWhiteSpace(dto.LastName) ? user?.LastName : dto.LastName;

    // 3) Load basket
    var basket = await _basketRead.GetAsync(
        b => !b.IsDeleted &&
             ((uid != null && b.UserId == uid) ||
              (uid == null && b.FootprintId == dto.FootprintId)),
        include: q => q
            .Include(b => b.BasketItems.Where(x => !x.IsDeleted))
                .ThenInclude(bi => bi.Product)
                    .ThenInclude(p => p.Images.Where(i => !i.IsDeleted))
            .Include(b => b.BasketItems.Where(x => !x.IsDeleted))
                .ThenInclude(bi => bi.ProductVariant),
        enableTracking: true
    );

    if (basket?.BasketItems == null || basket.BasketItems.Count == 0)
        throw new GlobalAppException("BASKET_EMPTY");

    // 4) Transaction
    var db = _orderWrite.GetDbContext();
    await using var tx = await db.Database.BeginTransactionAsync();

    try
    {
        var now = DateTime.UtcNow;

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = uid,
            FootprintId = uid == null ? dto.FootprintId : null,
            OrderNumber = GenerateOrderNumber(),

            Email = resolvedEmail.Trim(),
            Phone = resolvedPhone,
            FirstName = resolvedFirstName,
            LastName = resolvedLastName,

            ShippingAddressLineOne = dto.ShippingAddressLineOne,
            ShippingAddressLineTwo = dto.ShippingAddressLineTwo,
            ShippingCity = dto.ShippingCity,
            ShippingState = dto.ShippingState,
            ShippingCountry = dto.ShippingCountry,
            ShippingPostalCode = dto.ShippingPostalCode,

            BillingAddressLineOne = dto.BillingAddressLineOne,
            BillingAddressLineTwo = dto.BillingAddressLineTwo,
            BillingCity = dto.BillingCity,
            BillingState = dto.BillingState,
            BillingCountry = dto.BillingCountry,
            BillingPostalCode = dto.BillingPostalCode,

            Status = "pending",
            PaymentStatus = "pending",
            FulfillmentStatus = "unfulfilled",

            DeliveryMethod = dto.DeliveryMethod,
            DeliveryNote = dto.DeliveryNote,

            SubTotal = 0m,
            DiscountAmount = 0m,
            ShippingCost = dto.ShippingCost ?? 0m,
            TaxAmount = 0m,
            Total = 0m,

            CreatedDate = now,
            LastUpdatedDate = now,
            IsDeleted = false,

            OrderItems = new List<OrderItem>()
        };

        decimal subTotal = 0m;

        foreach (var bi in basket.BasketItems.Where(x => !x.IsDeleted))
        {
            var qty = bi.Quantity;
            if (qty <= 0) continue;

            // product
            var product = bi.Product;
            if (product == null)
            {
                product = await _productRead.GetAsync(
                    p => p.Id == bi.ProductId && !p.IsDeleted,
                    include: q => q.Include(x => x.Images.Where(i => !i.IsDeleted)),
                    enableTracking: true
                );
            }

            if (product == null)
                throw new GlobalAppException("PRODUCT_NOT_FOUND");

            // variant
            ProductVariant? pv = null;
            if (bi.ProductVariantId.HasValue)
            {
                pv = bi.ProductVariant;
                if (pv == null)
                {
                    pv = await _pvRead.GetAsync(
                        x => x.Id == bi.ProductVariantId.Value && !x.IsDeleted,
                        enableTracking: true
                    );
                }

                if (pv == null)
                    throw new GlobalAppException("VARIANT_NOT_FOUND");
            }

            var priceModifier = pv?.PriceModifier ?? 0m;
            var unitPrice = product.Price + priceModifier;

            if (unitPrice < 0)
                unitPrice = 0;

            var lineSubTotal = unitPrice * qty;

            // stock
            if (pv != null)
            {
                if (!pv.IsAvailable || pv.StockQuantity < qty)
                    throw new GlobalAppException("OUT_OF_STOCK");

                pv.StockQuantity -= qty;
                pv.LastUpdatedDate = now;

                if (pv.StockQuantity <= 0)
                {
                    pv.StockQuantity = 0;
                    pv.IsAvailable = false;
                }

                await _pvWrite.UpdateAsync(pv);
            }
            else
            {
                if (product.TrackInventory == true)
                {
                    var allowBackorder = product.AllowBackorder ?? false;

                    if (!allowBackorder)
                    {
                        if (!product.StockQuantity.HasValue || product.StockQuantity.Value < qty)
                            throw new GlobalAppException("OUT_OF_STOCK");
                    }

                    if (product.StockQuantity.HasValue)
                    {
                        product.StockQuantity -= qty;
                        if (product.StockQuantity.Value < 0)
                            product.StockQuantity = 0;
                    }

                    product.LastUpdatedDate = now;
                    await _productWrite.UpdateAsync(product);
                }
            }

            var primaryImg =
                product.Images?.FirstOrDefault(x => !x.IsDeleted && x.IsPrimary)?.ImageUrl
                ?? product.Images?.FirstOrDefault(x => !x.IsDeleted)?.ImageUrl;

            var oi = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,

                ProductId = product.Id,
                ProductVariantId = pv?.Id,

                ProductName = FirstNonEmpty(product.NameAz, product.NameEn, product.NameTr, product.NameRu) ?? "Product",
                ProductSku = product.Sku,
                ProductVariantName = pv?.VariantSlug,

                UnitPrice = unitPrice,
                Quantity = qty,

                // satış qiyməti ilə hesablanır
                SubTotal = lineSubTotal,
                DiscountAmount = 0m,
                Total = lineSubTotal,

                ProductImageUrl = primaryImg,

                CreatedDate = now,
                LastUpdatedDate = now,
                IsDeleted = false
            };

            product.OrderCount += qty;
            product.LastUpdatedDate = now;
            await _productWrite.UpdateAsync(product);

            order.OrderItems.Add(oi);
            subTotal += lineSubTotal;
        }

        if (order.OrderItems.Count == 0)
            throw new GlobalAppException("BASKET_EMPTY");

        // Coupon
        decimal couponDiscount = 0m;

        if (!string.IsNullOrWhiteSpace(dto.CouponCode))
        {
            var result = await ApplyCouponAsync(
                couponCode: dto.CouponCode,
                subTotal: subTotal,
                userId: uid,
                footprintId: dto.FootprintId,
                now: now
            );

            couponDiscount = result.discount;
            order.CuponCode = result.appliedCode;
        }

        // Totals
        order.SubTotal = subTotal;
        order.DiscountAmount = couponDiscount;
        order.TaxAmount = 0m;

        order.Total =
            order.SubTotal
            - (order.DiscountAmount ?? 0m)
            + (order.ShippingCost ?? 0m)
            + (order.TaxAmount ?? 0m);

        if (order.Total < 0)
            order.Total = 0;

        // Save order
        await _orderWrite.AddAsync(order);

        await _cuponWrite.CommitAsync();
        await _orderWrite.CommitAsync();

        await MarkAbandonedCartRecoveredAsync(order.Id, basket.Id, uid, dto.FootprintId);

        // Clear basket
        foreach (var bi in basket.BasketItems.Where(x => !x.IsDeleted))
        {
            bi.IsDeleted = true;
            bi.DeletedDate = now;
            bi.LastUpdatedDate = now;
        }

        basket.LastUpdatedDate = now;
        await _basketWrite.UpdateAsync(basket);
        await _basketWrite.CommitAsync();

        await tx.CommitAsync();

        return _mapper.Map<OrderCreatedDto>(order);
    }
    catch
    {
        await tx.RollbackAsync();
        throw;
    }
}

    
    // ==========================================================
    // COUPON APPLY
    // ==========================================================
    private async Task<(decimal discount, string appliedCode)> ApplyCouponAsync(
        string couponCode,
        decimal subTotal,
        Guid? userId,
        string? footprintId,
        DateTime now)
    {
        var code = couponCode.Trim();

        var coupon = await _cuponRead.GetAsync(
            x => !x.IsDeleted && x.IsActive && x.Code.ToLower() == code.ToLower(),
            enableTracking: true
        );

        if (coupon == null) throw new GlobalAppException("NOT_FOUND_CUPON");

        if (coupon.ValidFrom.HasValue && now < coupon.ValidFrom.Value)
            throw new GlobalAppException("COUPON_NOT_STARTED");

        if (coupon.ValidUntil.HasValue && now > coupon.ValidUntil.Value)
            throw new GlobalAppException("COUPON_EXPIRED");

        if (coupon.MinimumPurchaseAmount.HasValue && subTotal < coupon.MinimumPurchaseAmount.Value)
            throw new GlobalAppException("COUPON_MIN_AMOUNT");

        if (coupon.MaxUses.HasValue && coupon.CurrentUses >= coupon.MaxUses.Value)
            throw new GlobalAppException("COUPON_USAGE_LIMIT");

        // per-user / per-footprint limit
        if (coupon.MaxUsesPerUser > 0)
        {
            var db = _orderWrite.GetDbContext();

            var usedCount = await db.Set<Order>().CountAsync(o =>
                !o.IsDeleted &&
                o.CuponCode != null &&
                o.CuponCode.ToLower() == coupon.Code.ToLower() &&
                (
                    (userId != null && o.UserId == userId) ||
                    (userId == null && footprintId != null && o.FootprintId == footprintId)
                )
            );

            if (usedCount >= coupon.MaxUsesPerUser)
                throw new GlobalAppException("COUPON_USER_LIMIT");
        }

        // ✅ DiscountType robust parsing
        // DB-də "percentage"/"fixed_amount" ola bilər
        // və ya "Percentage"/"FixedAmount"
        // və ya "0"/"1"
        decimal discount = 0m;

        var t = (coupon.DiscountType ?? "").Trim().ToLowerInvariant();

        var isPercentage =
            t is "percentage" or "percent" or "0" or "percentage " or "percentage," ||
            t.Contains("percentage") || t.Contains("percent");

        var isFixed =
            t is "fixed_amount" or "fixedamount" or "fixed" or "1" ||
            t.Contains("fixed");

        if (isPercentage)
            discount = Math.Round(subTotal * (coupon.DiscountValue / 100m), 2);
        else if (isFixed)
            discount = coupon.DiscountValue;
        else
            throw new GlobalAppException("INVALID_COUPON_TYPE");

        if (discount <= 0m) return (0m, coupon.Code);
        if (discount > subTotal) discount = subTotal;

        coupon.CurrentUses += 1;
        coupon.LastUpdatedDate = now;
        await _cuponWrite.UpdateAsync(coupon);

        return (discount, coupon.Code);
    }

    // ==========================================================
    // USER ORDERS LIST (FILTER: status + date range)
    // ==========================================================
    public async Task<List<OrderDto>> GetMyOrdersAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var uid))
            throw new GlobalAppException("INVALID_USER_ID");

        var orders = await _orderRead.GetAllAsync(
            o => !o.IsDeleted && o.UserId == uid,
            include: q => q.Include(x => x.OrderItems.Where(i => !i.IsDeleted)),
            enableTracking: false
        );

        orders = orders.OrderByDescending(x => x.CreatedDate).ToList();
        return _mapper.Map<List<OrderDto>>(orders);
    }

    // ==========================================================
    // ORDER DETAIL


    public async Task<OrderDetailDto> GetOrderDetailAsync(string userId, string orderId)
    {
        if (!Guid.TryParse(userId, out var uid))
            throw new GlobalAppException("INVALID_USER_ID");
        if (!Guid.TryParse(orderId, out var oid))
            throw new GlobalAppException("INVALID_ORDER_ID");

        var order = await _orderRead.GetAsync(
            o => !o.IsDeleted && o.Id == oid && o.UserId == uid,
            include: q => q.Include(x => x.OrderItems.Where(i => !i.IsDeleted)),
            enableTracking: false
        );

        if (order == null)
            throw new GlobalAppException("ORDER_NOT_FOUND");

        return _mapper.Map<OrderDetailDto>(order);
    }

    public async Task<List<OrderDto>> GetMyOrdersByStatusAsync(string userId, string status)
    {
        if (!Guid.TryParse(userId, out var uid))
            throw new GlobalAppException("INVALID_USER_ID");

        if (!IsValidStatus(status))
            throw new GlobalAppException("INVALID_STATUS");

        var s = NormalizeStatus(status);

        var orders = await _orderRead.GetAllAsync(
            o => !o.IsDeleted && o.UserId == uid && o.Status.ToLower() == s,
            include: q => q.Include(x => x.OrderItems.Where(i => !i.IsDeleted)),
            enableTracking: false
        );

        orders = orders.OrderByDescending(x => x.CreatedDate).ToList();
        return _mapper.Map<List<OrderDto>>(orders);
    }

// =========================
// DATE FILTER
// =========================
    public async Task<List<OrderDto>> GetMyOrdersByDateAsync(string userId, string dateFilter)
    {
        if (!Guid.TryParse(userId, out var uid))
            throw new GlobalAppException("INVALID_USER_ID");

        var now = DateTime.UtcNow;
        var from = ResolveDateFrom(dateFilter, now);

        if (from == null)
            throw new GlobalAppException("INVALID_DATE_FILTER"); // bunu message-lərə əlavə edəcəyik

        var orders = await _orderRead.GetAllAsync(
            o => !o.IsDeleted && o.UserId == uid && o.CreatedDate >= from.Value,
            include: q => q.Include(x => x.OrderItems.Where(i => !i.IsDeleted)),
            enableTracking: false
        );

        orders = orders.OrderByDescending(x => x.CreatedDate).ToList();
        return _mapper.Map<List<OrderDto>>(orders);
    }
    // ==========================================================
    // ADMIN: UPDATE STATUS (admin paneldən)
    // ==========================================================
    public async Task SetOrderStatusAsync(string orderId, string newStatus)
    {
        if (!Guid.TryParse(orderId, out var oid))
            throw new GlobalAppException("INVALID_ORDER_ID");

        var status = (newStatus ?? "").Trim().ToLowerInvariant();
        if (status is not ("pending" or "delivered" or "cancelled"))
            throw new GlobalAppException("INVALID_STATUS");

        var order = await _orderWrite.GetDbContext().Set<Order>()
            .FirstOrDefaultAsync(o => !o.IsDeleted && o.Id == oid);

        if (order == null)
            throw new GlobalAppException("ORDER_NOT_FOUND");

        order.Status = status;
        order.LastUpdatedDate = DateTime.UtcNow;

        await _orderWrite.UpdateAsync(order);
        await _orderWrite.CommitAsync();
        await _campaignService.CreateAndScheduleOrderStatusCampaignAsync(order);
    }
    private async Task MarkAbandonedCartRecoveredAsync(Guid orderId, Guid? basketId, Guid? userId, string? footprintId)
    {
        var db = _orderWrite.GetDbContext();
        var now = DateTime.UtcNow;

        var q = db.Set<AbandonedCart>().Where(a =>
            !a.IsDeleted &&
            a.Status != "recovered"&&
            (
                (basketId != null && a.BasketId == basketId) ||
                (basketId == null && userId != null && a.UserId == userId) ||
                (basketId == null && userId == null && footprintId != null && a.FootprintId == footprintId)
            )
        );

        var hit = await q
            .OrderByDescending(a => a.CreatedDate)
            .FirstOrDefaultAsync();

        if (hit == null) return;

        hit.Status ="recovered";
        hit.ConvertedToOrderId = orderId;
        hit.LastUpdatedDate = now;

        await db.SaveChangesAsync();
    }
    private static string NormalizeStatus(string status)
        => (status ?? "").Trim().ToLowerInvariant();

    private static DateTime? ResolveDateFrom(string? dateFilter, DateTime nowUtc)
    {
        return (dateFilter ?? "").Trim().ToLowerInvariant() switch
        {
            "week" => nowUtc.AddDays(-7),
            "month" => nowUtc.AddMonths(-1),
            "year" => nowUtc.AddYears(-1),
            _ => null
        };
    }

    private static bool IsValidStatus(string status)
    {
        var s = NormalizeStatus(status);
        return s is "pending" or "delivered" or "cancelled";
    }

    private static string GenerateOrderNumber()
        => $"MZ-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    private static string? FirstNonEmpty(params string?[] vals)
        => vals.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();
}
