using MezuroApp.Application.Abstracts.Repositories.ProductColorImages;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.ProductColorImages;

public class ProductColorImageWriteRepository:WriteRepository<ProductColorImage>,IProductColorImageWriteRepository
{
    public ProductColorImageWriteRepository(MezuroAppDbContext MezuroAppDbContext) : base(MezuroAppDbContext)
    {
    }
}