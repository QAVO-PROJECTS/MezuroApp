using MezuroApp.Application.Abstracts.Repositories;
using MezuroApp.Application.Abstracts.Repositories.Categories;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;


namespace MezuroApp.Persistance.Concretes.Repositories.Categories
{
    public class CategoryReadRepository : ReadRepository<Category>, ICategoryReadRepository
    {
        public CategoryReadRepository(MezuroAppDbContext mezuroAppDbContext) : base(mezuroAppDbContext)
        {
        }
    }
}
