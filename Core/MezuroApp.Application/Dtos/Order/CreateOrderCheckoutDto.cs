namespace MezuroApp.Application.Dtos.Order;

public class CreateOrderCheckoutDto
{
    // Guest checkout üçün (login deyilsə mütləq)
    public string? FootprintId { get; set; }

    // Contact
    public string? Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    // Shipping
    public string? ShippingAddressLineOne { get; set; }
    public string? ShippingAddressLineTwo { get; set; }
    public string? ShippingCity { get; set; }
    public string? ShippingState { get; set; }
    public string? ShippingCountry { get; set; }
    public string? ShippingPostalCode { get; set; }

    // Billing (optional)
    public string? BillingAddressLineOne { get; set; }
    public string? BillingAddressLineTwo { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingState { get; set; }
    public string? BillingCountry { get; set; }
    public string? BillingPostalCode { get; set; }

    // Delivery
    public string? DeliveryMethod { get; set; }
    public string? DeliveryNote { get; set; }
    public decimal? ShippingCost { get; set; }
    public string? CouponCode { get; set; }

}