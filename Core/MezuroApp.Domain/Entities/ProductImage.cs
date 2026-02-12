using System.Drawing;
using MezuroApp.Domain.Entities.Common;

namespace MezuroApp.Domain.Entities;

public class ProductImage:BaseEntity
{
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }


    public string ImageUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? AltText { get; set; }

    public bool IsPrimary { get; set; }
    public int? SortOrder { get; set; }
 
   public List<ProductColorImage>? ColorImages { get; set; }
}