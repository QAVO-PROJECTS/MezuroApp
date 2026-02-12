using MezuroApp.Application.Abstracts.Repositories.Options;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.Options;

public class OptionReadRepository:ReadRepository<Option>,IOptionReadRepository
{
    public OptionReadRepository(MezuroAppDbContext dbContext) : base(dbContext)
    {
    }
}