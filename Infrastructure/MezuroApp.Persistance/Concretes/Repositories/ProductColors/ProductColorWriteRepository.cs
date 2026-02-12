using MezuroApp.Application.Abstracts.Repositories.ProductColors;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.ProductColors;

public class ProductColorWriteRepository:WriteRepository<ProductColor>,IProductColorWriteRepository
{
    public ProductColorWriteRepository(MezuroAppDbContext MezuroAppDbContext) : base(MezuroAppDbContext)
    {
    }
}