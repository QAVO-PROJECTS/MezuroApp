using MezuroApp.Application.Abstracts.Repositories.ProductCategories;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.ProductCategories;

public class ProductCategoryWriteRepository:WriteRepository<ProductCategory>,IProductCategoryWriteRepository
    
{
    public ProductCategoryWriteRepository(MezuroAppDbContext MezuroAppDbContext) : base(MezuroAppDbContext)
    {
    }
}