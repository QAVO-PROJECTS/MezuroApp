using MezuroApp.Application.Abstracts.Repositories.Reviews;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.Reviews;

public class ReviewReadRepository:ReadRepository<Review>,IReviewReadRepository
{
    public ReviewReadRepository(MezuroAppDbContext dbContext) : base(dbContext)
    {
    }
}