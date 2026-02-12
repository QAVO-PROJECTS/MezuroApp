using MezuroApp.Application.Abstracts.Repositories.ProductColors;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.ProductColors;

public class ProductColorReadRepository:ReadRepository<ProductColor>,IProductColorReadRepository
{
    public ProductColorReadRepository(MezuroAppDbContext dbContext) : base(dbContext)
    {
    }
}