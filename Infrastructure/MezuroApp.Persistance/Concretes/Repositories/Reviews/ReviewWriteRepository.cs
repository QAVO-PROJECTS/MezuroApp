using MezuroApp.Application.Abstracts.Repositories.Reviews;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.Reviews;

public class ReviewWriteRepository:WriteRepository<Review>,IReviewWriteRepository
{
    public ReviewWriteRepository(MezuroAppDbContext MezuroAppDbContext) : base(MezuroAppDbContext)
    {
    }
}