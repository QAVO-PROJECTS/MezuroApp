using MezuroApp.Application.Abstracts.Repositories.Wishlists;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.Wishlists;

public class WishlistReadRepository:ReadRepository<Wishlist>,IWishlistReadRepository
{
    public WishlistReadRepository(MezuroAppDbContext dbContext) : base(dbContext)
    {
    }
}