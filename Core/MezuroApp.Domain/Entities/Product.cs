using MezuroApp.Domain.Entities.Common;

namespace MezuroApp.Domain.Entities;

public class Product:BaseEntity
{
    public string NameAz { get; set; }
    public string NameRu { get; set; }
    public string NameEn { get; set; }
    public string NameTr { get; set; }

    public string? DescriptionAz { get; set; }
    public string? DescriptionRu { get; set; }
    public string? DescriptionEn { get; set; }
    public string? DescriptionTr { get; set; }

    public string? ShortDescriptionAz { get; set; }
    public string? ShortDescriptionRu { get; set; }
    public string? ShortDescriptionEn { get; set; }
    public string? ShortDescriptionTr { get; set; }

    // Identifiers
    public string Sku { get; set; }
    public string? Barcode { get; set; }
    public string Slug { get; set; }

    // Pricing
    public decimal Price { get; set; }
    public string? Currency { get; set; }
    
    public decimal? CompareAtPrice { get; set; }
    public decimal? CostPrice { get; set; }

    // Inventory
    public int? StockQuantity { get; set; }
    public int? LowStockThreshold { get; set; }
    public bool? TrackInventory { get; set; }
    public bool? AllowBackorder { get; set; }

    // Status
    public bool? IsActive { get; set; }
    public bool? IsFeatured { get; set; }
    public bool? IsNew { get; set; }
    public bool? IsOnSale { get; set; }

    // Physical
    public decimal? Weight { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }

    // Ratings
    public decimal RatingAverage { get; set; }
    public int RatingCount { get; set; }
    public int ReviewCount { get; set; }

    // Analytics
    public int ViewCount { get; set; }
    public int WishlistCount { get; set; }
    public int OrderCount { get; set; }

    // SEO
    public string? MetaTitleAz { get; set; }
    public string? MetaTitleRu { get; set; }
    public string? MetaTitleEn { get; set; }
    public string? MetaTitleTr { get; set; }

    public string? MetaDescriptionAz { get; set; }
    public string? MetaDescriptionRu { get; set; }
    public string? MetaDescriptionEn { get; set; }
    public string? MetaDescriptionTr { get; set; }


    public DateTime? PublishedAt { get; set; }
    // Relations
    public List<ProductCategory>? ProductCategories { get; set; }
    public List<ProductImage>? Images { get; set; }
    public List<ProductColor>? ProductColors { get; set; }
    public List<ProductOption>? Options { get; set; }
    public List<ProductVariant>? Variants { get; set; }
}