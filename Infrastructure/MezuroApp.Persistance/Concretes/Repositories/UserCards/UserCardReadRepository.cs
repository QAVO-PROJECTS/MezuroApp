using MezuroApp.Application.Abstracts.Repositories.UserCards;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.UserCards;

public class UserCardReadRepository:ReadRepository<UserCard>,IUserCardReadRepository
    
{
    public UserCardReadRepository(MezuroAppDbContext dbContext) : base(dbContext)
    {
    }
}