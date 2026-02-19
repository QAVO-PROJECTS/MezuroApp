using MezuroApp.Application.Abstracts.Repositories.OrderItems;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.OrderItems;

public class OrderItemWriteRepository:WriteRepository<OrderItem>,IOrderItemWriteRepository
{
    public OrderItemWriteRepository(MezuroAppDbContext MezuroAppDbContext) : base(MezuroAppDbContext)
    {
    }
}