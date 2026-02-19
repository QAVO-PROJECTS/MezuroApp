using MezuroApp.Application.Abstracts.Repositories.Cupons;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.Cupons;

public class CuponReadRepository:ReadRepository<Cupon>,ICuponReadRepository
{
    public CuponReadRepository(MezuroAppDbContext dbContext) : base(dbContext)
    {
    }
}