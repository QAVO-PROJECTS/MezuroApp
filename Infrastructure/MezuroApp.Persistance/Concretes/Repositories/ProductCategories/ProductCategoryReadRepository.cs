using MezuroApp.Application.Abstracts.Repositories.ProductCategories;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.ProductCategories;

public class ProductCategoryReadRepository:ReadRepository<ProductCategory>,IProductCategoryReadRepository
{
    public ProductCategoryReadRepository(MezuroAppDbContext MezuroAppDbContext) : base(MezuroAppDbContext)
    {
    }
}