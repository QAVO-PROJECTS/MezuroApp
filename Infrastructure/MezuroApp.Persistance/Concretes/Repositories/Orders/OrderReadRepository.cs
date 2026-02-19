using MezuroApp.Application.Abstracts.Repositories.Orders;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.Orders;

public class OrderReadRepository:ReadRepository<Order>,IOrderReadRepository
    
{
    public OrderReadRepository(MezuroAppDbContext dbContext) : base(dbContext)
    {
    }
}