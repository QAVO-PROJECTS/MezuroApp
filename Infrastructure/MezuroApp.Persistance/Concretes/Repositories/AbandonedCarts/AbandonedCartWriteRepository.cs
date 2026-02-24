using MezuroApp.Application.Abstracts.Repositories;
using MezuroApp.Application.Abstracts.Repositories.AbandonedCarts;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.AbandonedCarts;

public class AbandonedCartWriteRepository:WriteRepository<AbandonedCart>,IAbandonedCartWriteRepository
{
    public AbandonedCartWriteRepository(MezuroAppDbContext MezuroAppDbContext) : base(MezuroAppDbContext)
    {
    }
}