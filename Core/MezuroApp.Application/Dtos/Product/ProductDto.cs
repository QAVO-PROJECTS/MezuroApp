using System.ComponentModel.DataAnnotations.Schema;
using MezuroApp.Application.Dtos.Category;
using MezuroApp.Application.Dtos.Option;
using MezuroApp.Application.Dtos.ProductColor;
using MezuroApp.Application.Dtos.ProductOption;
using MezuroApp.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Newtonsoft.Json.Linq;

namespace MezuroApp.Application.Dtos.Product;

public class ProductDto
{
    public string Id { get; set; }
    public string? NameAz { get; set; }
    public string? NameRu { get; set; }
    public string? NameEn { get; set; }
    public string? NameTr { get; set; }

    public string? DescriptionAz { get; set; }
    public string? DescriptionRu { get; set; }
    public string? DescriptionEn { get; set; }
    public string? DescriptionTr { get; set; }

    public string? ShortDescriptionAz { get; set; }
    public string? ShortDescriptionRu { get; set; }
    public string? ShortDescriptionEn { get; set; }
    public string? ShortDescriptionTr { get; set; }

    // Identifiers
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public string? Slug { get; set; }

    // Pricing
    public decimal? Price { get; set; }
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
    public bool? IsBestseller { get; set; }


    public int ReviewCount { get; set; }
    public int ViewCount { get; set; }
    public int WishlistCount { get; set; }
    public decimal? RatingAverage { get; set; }

    // Ratings


    // SEO
    public string? MetaTitleAz { get; set; }
    public string? MetaTitleRu { get; set; }
    public string? MetaTitleEn { get; set; }
    public string? MetaTitleTr { get; set; }

    public string? MetaDescriptionAz { get; set; }
    public string? MetaDescriptionRu { get; set; }
    public string? MetaDescriptionEn { get; set; }
    public string? MetaDescriptionTr { get; set; }
    public List<ProductImageDto>? Images { get; set; }
    public List<ProductOptionDto>? Options { get; set; }
    public List<ProductColorDto>? Colors { get; set; }
    // Relations
    public List<CategoryDto>? Categories { get; set; }
    
}
