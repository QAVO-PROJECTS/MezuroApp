using MezuroApp.Application.Abstracts.Repositories.Addresses;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.Addresses;

public class AddressWriteRepository:WriteRepository<UserAddress>,IAddressWriteRepository
{
    public AddressWriteRepository(MezuroAppDbContext MezuroAppDbContext) : base(MezuroAppDbContext)
    {
    }
}