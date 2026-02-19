using MezuroApp.Application.Abstracts.Repositories.BasketItems;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.BasketItems;

public class BasketItemReadRepository:ReadRepository<BasketItem>,IBasketItemReadRepository
{
    public BasketItemReadRepository(MezuroAppDbContext dbContext) : base(dbContext)
    {
    }
}