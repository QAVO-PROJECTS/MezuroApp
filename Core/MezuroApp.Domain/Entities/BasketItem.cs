using MezuroApp.Domain.Entities.Common;

namespace MezuroApp.Domain.Entities;

public class BasketItem:BaseEntity
{
    public Guid? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public Guid BasketId { get; set; }
    public Basket? Basket { get; set; }
    public int Quantity { get; set; }
    
}