using MezuroApp.Domain.Entities.Common;

namespace MezuroApp.Domain.Entities;

public sealed class UserCard : BaseEntity
{
    
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string? CardUid { get; set; } // varchar(255)

    public string? CardName { get; set; }
    public string? CardMask { get; set; }
    public string? CardExpiry { get; set; }

    public string? BankTransaction { get; set; } // text
    public string? BankResponse { get; set; }    // text
    public string? OperationCode { get; set; }
    public string? Rrn { get; set; }

    public bool IsDefault { get; set; } = false;

    // Soft delete
    public DateTime? DeletedAt { get; set; }
    public List<PaymentTransaction>? PaymentTransactions { get; set; }
}