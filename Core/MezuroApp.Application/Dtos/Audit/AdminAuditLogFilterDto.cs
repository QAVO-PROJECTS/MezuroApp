namespace MezuroApp.Application.Dtos.Audit;

public sealed class AdminAuditLogFilterDto
{
    public string? Search { get; set; }     // admin name, entity, orderNumber, couponCode...
    public string? AdminId { get; set; }    // userId (string)
    public string? Module { get; set; }     // orders/products/categories/coupons/options/transactions/reviews
    public string? Action { get; set; }     // create/update/delete  (və ya ALL)

    public string? From { get; set; }       // dd.MM.yyyy
    public string? To { get; set; }         // dd.MM.yyyy

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}