using MezuroApp.Application.Abstracts.Repositories.ProductVariants;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.ProductVariants;

public class ProductVariantWriteRepository:WriteRepository<ProductVariant>,IProductVariantWriteRepository
{
    public ProductVariantWriteRepository(MezuroAppDbContext MezuroAppDbContext) : base(MezuroAppDbContext)
    {
    }
}