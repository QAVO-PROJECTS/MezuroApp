using MezuroApp.Application.Abstracts.Repositories.OptionValues;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.OptionValues;

public class ProductOptionValueWriteRepository:WriteRepository<ProductOptionValue>,IProductOptionValueWriteRepository
{
    public ProductOptionValueWriteRepository(MezuroAppDbContext MezuroAppDbContext) : base(MezuroAppDbContext)
    {
    }
}