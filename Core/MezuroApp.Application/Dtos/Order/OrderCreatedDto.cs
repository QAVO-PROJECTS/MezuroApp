namespace MezuroApp.Application.Dtos.Order;

public class OrderCreatedDto
{
    public string OrderId { get; set; } = null!;
    public string OrderNumber { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string PaymentStatus { get; set; } = null!;
    public string FulfillmentStatus { get; set; } = null!;

    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }

    public List<OrderItemCreatedDto> Items { get; set; } = new();
}

