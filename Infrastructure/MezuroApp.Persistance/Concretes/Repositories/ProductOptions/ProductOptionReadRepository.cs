using MezuroApp.Application.Abstracts.Repositories.ProductOptions;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.ProductOptions;

public class ProductOptionReadRepository:ReadRepository<ProductOption>,IProductOptionReadRepository
{
    public ProductOptionReadRepository(MezuroAppDbContext dbContext) : base(dbContext)
    {
    }
}