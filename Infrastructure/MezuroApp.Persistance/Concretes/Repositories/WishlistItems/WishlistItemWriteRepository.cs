using MezuroApp.Application.Abstracts.Repositories.WishlistItems;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.WishlistItems;

public class WishlistItemWriteRepository:WriteRepository<WishlistItem>,IWishlistItemWriteRepository
{
    public WishlistItemWriteRepository(MezuroAppDbContext MezuroAppDbContext) : base(MezuroAppDbContext)
    {
    }
}