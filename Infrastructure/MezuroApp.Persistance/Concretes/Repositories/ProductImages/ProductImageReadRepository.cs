using MezuroApp.Application.Abstracts.Repositories.ProductImages;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.ProductImages;

public class ProductImageReadRepository:ReadRepository<ProductImage>,IProductImageReadRepository
{
    public ProductImageReadRepository(MezuroAppDbContext dbContext) : base(dbContext)
    {
    }
}