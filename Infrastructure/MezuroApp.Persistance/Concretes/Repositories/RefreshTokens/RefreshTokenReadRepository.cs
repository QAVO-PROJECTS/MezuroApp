using MezuroApp.Application.Abstracts.Repositories.RefreshTokens;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.RefreshTokens;

public class RefreshTokenReadRepository:ReadRepository<RefreshToken>,IRefreshTokenReadRepository
{
    public RefreshTokenReadRepository(MezuroAppDbContext dbContext) : base(dbContext)
    {
    }
}