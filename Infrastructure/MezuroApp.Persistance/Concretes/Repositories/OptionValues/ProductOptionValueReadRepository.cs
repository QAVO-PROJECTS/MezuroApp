using MezuroApp.Application.Abstracts.Repositories.OptionValues;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.OptionValues;

public class ProductOptionValueReadRepository:ReadRepository<ProductOptionValue>,IProductOptionValueReadRepository
{
    public ProductOptionValueReadRepository(MezuroAppDbContext dbContext) : base(dbContext)
    {
    }
}