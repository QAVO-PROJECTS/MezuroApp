using MezuroApp.Application.Abstracts.Repositories.Options;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.Options;

public class OptionWriteRepository:WriteRepository<Option>,IOptionWriteRepository
{
    public OptionWriteRepository(MezuroAppDbContext MezuroAppDbContext) : base(MezuroAppDbContext)
    {
    }
}