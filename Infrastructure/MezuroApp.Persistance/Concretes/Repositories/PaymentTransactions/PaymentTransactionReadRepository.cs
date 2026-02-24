using MezuroApp.Application.Abstracts.Repositories.PaymentTransactions;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.PaymentTransactions;

public class PaymentTransactionReadRepository:ReadRepository<PaymentTransaction>,IPaymentTransactionReadRepository
    
{
    public PaymentTransactionReadRepository(MezuroAppDbContext dbContext) : base(dbContext)
    {
    }
}