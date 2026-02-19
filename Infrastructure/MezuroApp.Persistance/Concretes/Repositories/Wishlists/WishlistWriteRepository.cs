using MezuroApp.Application.Abstracts.Repositories.Wishlists;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.Wishlists;

public class WishlistWriteRepository:WriteRepository<Wishlist>,IWishlistWriteRepository
{
    public WishlistWriteRepository(MezuroAppDbContext MezuroAppDbContext) : base(MezuroAppDbContext)
    {
    }
}