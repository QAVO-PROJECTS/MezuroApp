using MezuroApp.Application.Abstracts.Repositories.RefreshTokens;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.RefreshTokens;

public class RefreshTokenWriteRepository:WriteRepository<RefreshToken>,IRefreshTokenWriteRepository
{
    public RefreshTokenWriteRepository(MezuroAppDbContext MezuroAppDbContext) : base(MezuroAppDbContext)
    {
    }
}