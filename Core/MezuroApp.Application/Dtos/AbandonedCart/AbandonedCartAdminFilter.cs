namespace MezuroApp.Application.Dtos.AbandonedCart;

public sealed class AbandonedCartAdminFilter
{
    public string? Search { get; set; }           // email və ya footprint (istəsən)
    public string? Status { get; set; }           // created / sent / recovered (null -> hamısı)
    public string? CreatedFrom { get; set; }      // dd.MM.yyyy
    public string? CreatedUntil { get; set; }     // dd.MM.yyyy
    public string? ExpiryFrom { get; set; }       // dd.MM.yyyy
    public string? ExpiryUntil { get; set; }      // dd.MM.yyyy

    public bool? Recoverable { get; set; }        // true -> status != recovered && !expired
}