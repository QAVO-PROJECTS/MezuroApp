using System.Text.Json;
using MezuroApp.Application.Abstracts.Repositories.Categories;
using MezuroApp.Application.Abstracts.Repositories.Orders;
using MezuroApp.Application.Abstracts.Repositories.Products;

using MezuroApp.Application.Abstracts.Repositories.Cupons;
using MezuroApp.Application.Abstracts.Repositories.Options;
using MezuroApp.Application.Abstracts.Repositories.Reviews;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.GlobalException;

public sealed class AuditLookupService : IAuditLookupService
{
    private readonly IOrderReadRepository _orderRead;
    private readonly IProductReadRepository _productRead;
    private readonly ICuponReadRepository _couponRead;
    private readonly ICategoryReadRepository _categoryRead;
    private readonly IOptionReadRepository _optionRead;
    private readonly IReviewReadRepository _reviewRead;

    public AuditLookupService(
        IOrderReadRepository orderRead,
        IProductReadRepository productRead,
        ICuponReadRepository couponRead,
        ICategoryReadRepository categoryRead,
        IOptionReadRepository optionRead,
        IReviewReadRepository reviewRead)
    {
        _orderRead = orderRead;
        _productRead = productRead;
        _couponRead = couponRead;
        _categoryRead = categoryRead;
        _optionRead = optionRead;
        _reviewRead = reviewRead;
        
    }

    public async Task<Dictionary<string, object>?> GetOldValuesAsync(string entityType, string actionType, string? id)
    {
        // Create üçün old values lazım deyil
        if (actionType == "Create") return null;

        if (string.IsNullOrWhiteSpace(id) || !Guid.TryParse(id, out var gid))
            return null; // id yoxdursa old values də yoxdur

        entityType = (entityType ?? "").Trim().ToLowerInvariant();

        return entityType switch
        {
            "orders" => await GetOrderOldAsync(gid),
            "products" => await GetProductOldAsync(gid),
            "coupons" => await GetCouponOldAsync(gid),
            "categories"=> await GetCategoryOldAsync(gid),
            "options" => await GetOptionOldAsync(gid),
          "reviews" => await GetReviewOldAsync(gid),
            _ => null
        };
    }
    

    private async Task<Dictionary<string, object>?> GetOrderOldAsync(Guid id)
    {
        var o = await _orderRead.GetAsync(x => !x.IsDeleted && x.Id == id, enableTracking: false);
        if (o == null) return null;

        var snap = new
        {
            o.Id,
            o.OrderNumber,
            o.Status,
            o.PaymentStatus,
            o.FulfillmentStatus,
            o.Total,
            o.Email,
            o.Phone,
            o.CreatedDate
        };

        return ToDict(snap);
    }
     private async Task<Dictionary<string, object>?> GetReviewOldAsync(Guid id)
    {
        var o = await _reviewRead.GetAsync(x => !x.IsDeleted && x.Id == id, enableTracking: false);
        if (o == null) return null;

        var snap = new
        {
            o.Id,
        
            o.Status,
            o.AdminReplyDate,
            o.AdminReplyDescription,
            o.CreatedDate
        };

        return ToDict(snap);
    }
    private async Task<Dictionary<string, object>?> GetOptionOldAsync(Guid id)
    {
        var o = await _optionRead.GetAsync(x => !x.IsDeleted && x.Id == id, enableTracking: false);
        if (o == null) return null;

        var snap = new
        {
            o.Id,
            o.NameAz,
            o.NameEn,
            o.NameRu,
            o.NameTr,
            o.LastUpdatedDate,
            o.CreatedDate
        };

        return ToDict(snap);
    }
    private async Task<Dictionary<string, object>?> GetCategoryOldAsync(Guid id)
    {
        var o = await _categoryRead.GetAsync(x => !x.IsDeleted && x.Id == id, enableTracking: false);
        if (o == null) return null;

        var snap = new
        {
            o.Id,
            o.NameAz,
            o.NameEn,
            o.NameRu,
            o.NameTr,
            o.DescriptionAz,
            o.DescriptionEn,
            o.DescriptionRu,
            o.DescriptionTr,
            o.IsActive,
            o.CreatedDate,
            o.ImageAltText,
            o.ImageUrl,
            o.MetaDescriptionAz,
            o.MetaDescriptionEn,
            o.MetaDescriptionRu,
            o.MetaDescriptionTr,
            o.MetaTitleAz,
            o.MetaTitleEn,
            o.MetaTitleRu,
            o.MetaTitleTr,
            o.SubTitleAz,
            o.SubTitleEn,
            o.SubTitleRu,
            o.SubTitleTr,
            o.ShowInMenu
            
        };

        return ToDict(snap);
    }

    private async Task<Dictionary<string, object>?> GetProductOldAsync(Guid id)
    {
        var p = await _productRead.GetAsync(x => !x.IsDeleted && x.Id == id, enableTracking: false);
        if (p == null) return null;

        var snap = new
        {
            p.Id,
            p.NameAz,
            p.NameEn,
            p.Price,
            p.StockQuantity,
            p.IsActive,
            p.CreatedDate
        };

        return ToDict(snap);
    }

    private async Task<Dictionary<string, object>?> GetCouponOldAsync(Guid id)
    {
        var c = await _couponRead.GetAsync(x => !x.IsDeleted && x.Id == id, enableTracking: false);
        if (c == null) return null;

        var snap = new
        {
            c.Id,
            c.Code,
            c.DiscountType,
            c.DiscountValue,
            c.MinimumPurchaseAmount,
            c.MaxUses,
            c.IsActive,
            c.CreatedDate
        };

        return ToDict(snap);
    }

    private static Dictionary<string, object> ToDict(object obj)
    {
        // Reflection yazmırıq — ən stabil yol JSON -> Dictionary
        var json = JsonSerializer.Serialize(obj);
        return JsonSerializer.Deserialize<Dictionary<string, object>>(json)
               ?? new Dictionary<string, object>();
    }
}