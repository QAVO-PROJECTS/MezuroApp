using MezuroApp.Application.Abstracts.Repositories.WishlistItems;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.WishlistItems;

public class WishlistItemReadRepository:ReadRepository<WishlistItem>,IWishlistItemReadRepository
{
    public WishlistItemReadRepository(MezuroAppDbContext dbContext) : base(dbContext)
    {
    }
}