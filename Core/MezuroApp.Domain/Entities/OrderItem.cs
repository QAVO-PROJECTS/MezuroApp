using MezuroApp.Domain.Entities.Common;

namespace MezuroApp.Domain.Entities;

public class OrderItem:BaseEntity
{
    
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public Guid? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }
    public int Quantity { get; set; }
    public string ProductName { get; set; }
    public string? ProductVariantName { get; set; }
    public string? ProductSku { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal Total { get; set; }
    public string? ProductImageUrl { get; set; }
}