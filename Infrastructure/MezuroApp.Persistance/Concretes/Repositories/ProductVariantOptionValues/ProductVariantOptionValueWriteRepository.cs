using MezuroApp.Application.Abstracts.Repositories.ProductVariantOptionValues;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.ProductVariantOptionValues;

public class ProductVariantOptionValueWriteRepository:WriteRepository<ProductVariantOptionValue>,IProductVariantOptionValueWriteRepository
{
    public ProductVariantOptionValueWriteRepository(MezuroAppDbContext MezuroAppDbContext) : base(MezuroAppDbContext)
    {
    }
}