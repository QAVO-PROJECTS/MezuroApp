using MezuroApp.Domain.Entities.Common;

namespace MezuroApp.Domain.Entities;

public class Basket : BaseEntity
{
    public Guid? UserId { get; set; } // <- anonymous üçün nullable olsun
    public User? User { get; set; }

    public string? FootprintId { get; set; } // cihaz/browser üçün unikal id

    public List<BasketItem> BasketItems { get; set; } = new();
    public List<AbandonedCart> ? AbandonedCarts { get; set; }
}