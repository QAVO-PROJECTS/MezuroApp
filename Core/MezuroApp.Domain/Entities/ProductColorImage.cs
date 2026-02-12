using MezuroApp.Domain.Entities.Common;

namespace MezuroApp.Domain.Entities;

public class ProductColorImage:BaseEntity
{
    public Guid ProductColorId { get; set; }
    public ProductColor? ProductColor { get; set; }
    public Guid ProductImageId { get; set; }
    public ProductImage? ProductImage { get; set; }
}