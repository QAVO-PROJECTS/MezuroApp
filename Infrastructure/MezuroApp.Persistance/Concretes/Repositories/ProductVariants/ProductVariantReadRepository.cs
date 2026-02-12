using MezuroApp.Application.Abstracts.Repositories.ProductVariants;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.ProductVariants;

public class ProductVariantReadRepository:ReadRepository<ProductVariant>,IProductVariantReadRepository
{
    public ProductVariantReadRepository(MezuroAppDbContext dbContext) : base(dbContext)
    {
    }
}