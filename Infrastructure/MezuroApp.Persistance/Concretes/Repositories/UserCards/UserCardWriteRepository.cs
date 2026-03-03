using MezuroApp.Application.Abstracts.Repositories.UserCards;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.UserCards;

public class UserCardWriteRepository:WriteRepository<UserCard>,IUserCardWriteRepository
{
    public UserCardWriteRepository(MezuroAppDbContext MezuroAppDbContext) : base(MezuroAppDbContext)
    {
    }
}