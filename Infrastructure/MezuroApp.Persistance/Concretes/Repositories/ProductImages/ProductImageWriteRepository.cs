using MezuroApp.Application.Abstracts.Repositories.ProductImages;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.ProductImages;

public class ProductImageWriteRepository:WriteRepository<ProductImage>,IProductImageWriteRepository
{
    public ProductImageWriteRepository(MezuroAppDbContext MezuroAppDbContext) : base(MezuroAppDbContext)
    {
    }
}