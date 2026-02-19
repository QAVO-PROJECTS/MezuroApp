using MezuroApp.Domain.Entities.Common;

namespace MezuroApp.Domain.Entities;

public class Wishlist:BaseEntity
{

    public Guid UserId { get; set; }
    public Admin? User {  get; set; }

    public List<WishlistItem> Items { get; set; } = new List<WishlistItem>();
}