namespace MezuroApp.Application.Dtos.AbandonedCart;

public sealed class AbandonedCartStatsDto
{
    public int TotalAbandonedCarts { get; set; }
    public decimal PotentialRevenue { get; set; }
}