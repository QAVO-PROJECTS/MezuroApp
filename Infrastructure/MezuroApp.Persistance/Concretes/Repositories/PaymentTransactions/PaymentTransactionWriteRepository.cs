using MezuroApp.Application.Abstracts.Repositories.PaymentTransactions;
using MezuroApp.Domain.Entities;
using MezuroApp.Persistance.Context;

namespace MezuroApp.Persistance.Concretes.Repositories.PaymentTransactions;

public class PaymentTransactionWriteRepository:WriteRepository<PaymentTransaction>,IPaymentTransactionWriteRepository
{
    public PaymentTransactionWriteRepository(MezuroAppDbContext MezuroAppDbContext) : base(MezuroAppDbContext)
    {
    }
}