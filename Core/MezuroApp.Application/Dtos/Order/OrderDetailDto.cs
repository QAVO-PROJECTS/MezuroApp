namespace MezuroApp.Application.Dtos.Order;



public class OrderDetailDto
{
    public string OrderId { get; set; }
    public string OrderNumber { get; set; }

    public string Status { get; set; }
    public string PaymentStatus { get; set; }
    public string FulfillmentStatus { get; set; }

    public string? PaymentMethod { get; set; }

    public string CreatedDate { get; set; }

    // Address block
    public string Email { get; set; }
    public string? Phone { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public string? ShippingAddressLineOne { get; set; }
    public string? ShippingAddressLineTwo { get; set; }
    public string? ShippingCity { get; set; }
    public string? ShippingState { get; set; }
    public string? ShippingCountry { get; set; }
    public string? ShippingPostalCode { get; set; }

    public string? BillingAddressLineOne { get; set; }
    public string? BillingAddressLineTwo { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingState { get; set; }
    public string? BillingCountry { get; set; }
    public string? BillingPostalCode { get; set; }

    // pricing
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }

    public string? CouponCode { get; set; }
    public string? DeliveryMethod { get; set; }
    public string? DeliveryNote { get; set; }

    public List<OrderItemDto> Items { get; set; } = new();


}
