using MezuroApp.Application.Abstracts.Repositories.ProductOptions;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.ProductOptions;

public class ProductOptionWriteRepository:WriteRepository<ProductOption>,IProductOptionWriteRepository
{
    public ProductOptionWriteRepository(MezuroAppDbContext dbContext) : base(dbContext)
    {
    }
}