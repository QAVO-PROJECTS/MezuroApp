
using Order = MezuroApp.Domain.Entities.Order;

namespace MezuroApp.Application.Abstracts.Repositories.Orders;

public interface IOrderWriteRepository:IWriteRepository<Order>
{
    
}