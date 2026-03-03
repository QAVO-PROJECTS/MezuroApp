namespace MezuroApp.Application.Dtos.Transaction;

public sealed class AdminTransactionListFilterDto
{
    public string? Search { get; set; }              // order number / email / phone
    public string? PaymentMethod { get; set; }       // epoint / cod ...
    public string? Status { get; set; }              // pending/processing/completed/failed/refunded...

    // UI-dan dd.MM.yyyy gələcək (sən istəmişdin)
    public string? From { get; set; }                // dd.MM.yyyy
    public string? To { get; set; }                  // dd.MM.yyyy

    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}