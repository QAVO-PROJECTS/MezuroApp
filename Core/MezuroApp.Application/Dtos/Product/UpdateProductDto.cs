using System.ComponentModel.DataAnnotations.Schema;
using MezuroApp.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Newtonsoft.Json.Linq;

namespace MezuroApp.Application.Dtos.Product;

public class UpdateProductDto
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

    // Physical
    public decimal? Weight { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }

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
 
    public List<IFormFile>? NewImageFiles { get; set; }
    public List<IFormFile>? NewThumbnailImageFiles { get; set; }
    // JSON olaraq gələcək
    [FromForm(Name = "newImagesJson")]
    public string? NewImagesJson { get; set; } = "[]";

    [NotMapped]
    [BindNever]
    [ValidateNever]
    public List<UpdateProductImageMetaDto> NewImages
    {
        get
        {
            try
            {
                var token = JToken.Parse(NewImagesJson);
                if (token.Type == JTokenType.Array)
                    return token.ToObject<List<UpdateProductImageMetaDto>>();
                if (token.Type == JTokenType.Object)
                    return new List<UpdateProductImageMetaDto> { token.ToObject<UpdateProductImageMetaDto>() };
            }
            catch
            {
                // JSON parse alınmazsa boş siyahı
            }
            return new List<UpdateProductImageMetaDto>();
        }
    }

    public List<string>? DeleteImageIds { get; set; }
  
    // Relations
    public List<string>? NewProductCategoryIds { get; set; }
    public List<string>? DeleteCategoryIds { get; set; }
    
}
