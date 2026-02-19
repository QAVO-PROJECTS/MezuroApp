namespace MezuroApp.Application.Dtos.Order;

public class OrderItemCreatedDto
{
    public string OrderItemId { get; set; } = null!;
    public string ProductId { get; set; } = null!;
    public string? ProductVariantId { get; set; }

    public string ProductName { get; set; } = null!;
    public string? ProductSku { get; set; }
    public string? VariantName { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }

    public string? ProductImageUrl { get; set; }
}