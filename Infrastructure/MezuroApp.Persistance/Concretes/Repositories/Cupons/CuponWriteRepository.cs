using MezuroApp.Application.Abstracts.Repositories.Cupons;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.Cupons;

public class CuponWriteRepository:WriteRepository<Cupon>,ICuponWriteRepository
{
    public CuponWriteRepository(MezuroAppDbContext MezuroAppDbContext) : base(MezuroAppDbContext)
    {
    }
}