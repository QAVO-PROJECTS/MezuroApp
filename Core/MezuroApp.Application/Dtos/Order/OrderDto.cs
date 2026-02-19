namespace MezuroApp.Application.Dtos.Order;

public class OrderDto
{
    public string OrderId { get; set; }
    public string OrderNumber { get; set; }

    public string Status { get; set; }            // pending / delivered / cancelled
    public string PaymentStatus { get; set; }     // pending / paid / failed (səndə necədirsə)
    public string FulfillmentStatus { get; set; } // unfulfilled / fulfilled

    public string? PaymentMethod { get; set; }    // "card" / "debit card" / "cash" (hələlik string)

    public string CreatedDate { get; set; }

    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }   // item compare discount + coupon discount
    public decimal ShippingCost { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }

    // UI üçün: listdə 4 şəkil göstərəcəyik
    public List<string> PreviewImages { get; set; } = new();
}