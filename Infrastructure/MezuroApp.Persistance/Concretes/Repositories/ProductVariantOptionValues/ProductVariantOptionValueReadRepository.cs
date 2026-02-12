using MezuroApp.Application.Abstracts.Repositories.ProductVariantOptionValues;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.ProductVariantOptionValues;

public class ProductVariantOptionValueReadRepository:ReadRepository<ProductVariantOptionValue>,IProductVariantOptionValueReadRepository
{
    public ProductVariantOptionValueReadRepository(MezuroAppDbContext dbContext) : base(dbContext)
    {
    }
}