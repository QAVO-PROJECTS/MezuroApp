using MezuroApp.Application.Abstracts.Repositories.Addresses;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.Addresses;

public class AddressReadRepository:ReadRepository<UserAddress>,IAddressReadRepository
{
    public AddressReadRepository(MezuroAppDbContext dbContext) : base(dbContext)
    {
    }
}