using MezuroApp.Application.Abstracts.Repositories;
using MezuroApp.Application.Abstracts.Repositories.Products;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Concretes.Repositories;
using MezuroApp.Persistance.Context;


namespace MezuroApp.Persistance.Concretes.Repositories.Products
{
    public class ProductReadRepository : ReadRepository<Product>, IProductReadRepository
    {
        public ProductReadRepository(MezuroAppDbContext context) : base(context) { }
    }
}
