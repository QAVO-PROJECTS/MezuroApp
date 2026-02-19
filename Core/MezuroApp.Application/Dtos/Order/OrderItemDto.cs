namespace MezuroApp.Application.Dtos.Order;

public class OrderItemDto
{
    public string OrderItemId { get; set; }

    public string ProductId { get; set; }
    public string? ProductVariantId { get; set; }

    public string ProductName { get; set; }
    public string? VariantName { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }

    public decimal DiscountAmount { get; set; } // compare endirimi (line)
    public decimal Total { get; set; }

    public string? ProductImageUrl { get; set; }
}