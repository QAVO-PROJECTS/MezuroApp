using MezuroApp.Application.Abstracts.Repositories.BasketItems;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.BasketItems;

public class BasketItemWriteRepository:WriteRepository<BasketItem>,IBasketItemWriteRepository
    
{
    public BasketItemWriteRepository(MezuroAppDbContext MezuroAppDbContext) : base(MezuroAppDbContext)
    {
    }
}