namespace MezuroApp.Application.Dtos.Auth.Adress;

public class CreateAddressDto
{
    public string AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string City { get; set; }
    public string Country { get; set; }
    public string? PostalCode { get; set; }
}