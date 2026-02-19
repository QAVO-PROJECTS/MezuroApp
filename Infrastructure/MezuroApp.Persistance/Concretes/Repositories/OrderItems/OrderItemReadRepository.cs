using MezuroApp.Application.Abstracts.Repositories.OrderItems;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.OrderItems;

public class OrderItemReadRepository:ReadRepository<OrderItem>,IOrderItemReadRepository
{
    public OrderItemReadRepository(MezuroAppDbContext dbContext) : base(dbContext)
    {
    }
}