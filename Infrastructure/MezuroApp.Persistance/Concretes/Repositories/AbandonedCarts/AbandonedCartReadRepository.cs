using MezuroApp.Application.Abstracts.Repositories.AbandonedCarts;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.AbandonedCarts;

public class AbandonedCartReadRepository:ReadRepository<AbandonedCart>,IAbandonedCartReadRepository
    
{
    public AbandonedCartReadRepository(MezuroAppDbContext dbContext) : base(dbContext)
    {
    }
}