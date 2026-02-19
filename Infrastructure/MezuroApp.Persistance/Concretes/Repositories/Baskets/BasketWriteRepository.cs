using MezuroApp.Application.Abstracts.Repositories.Baskets;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.Baskets;

public class BasketWriteRepository:WriteRepository<Basket>,IBasketWriteRepository
{
    public BasketWriteRepository(MezuroAppDbContext MezuroAppDbContext) : base(MezuroAppDbContext)
    {
    }
}