using MezuroApp.Application.Abstracts.Repositories.Orders;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.Orders;

public class OrderWriteRepository:WriteRepository<Order>,IOrderWriteRepository
{
    public OrderWriteRepository(MezuroAppDbContext MezuroAppDbContext) : base(MezuroAppDbContext)
    {
    }
}