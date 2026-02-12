using MezuroApp.Application.Abstracts.Repositories.ProductColorImages;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.ProductColorImages;

public class ProductColorImageReadRepository:ReadRepository<ProductColorImage>,IProductColorImageReadRepository
{
    public ProductColorImageReadRepository(MezuroAppDbContext dbContext) : base(dbContext)
    {
    }
}