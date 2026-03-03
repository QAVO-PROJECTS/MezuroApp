using MezuroApp.Domain.Entities.Common;

namespace MezuroApp.Domain.Entities;

public class PaymentTransaction : BaseEntity
{
    // FK
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = default!;
    public Guid? UserCardId { get; set; } // NEW
    public UserCard? UserCard { get; set; } // optional
    public string? CardUid { get; set; }
    public bool SaveCardRequested { get; set; } 
    public decimal RefundedAmount { get; set; } = 0m;

    // Provider / Gateway
    public string PaymentMethod { get; set; } = "epoint"; // hələlik

    // Gateway transaction details
    public string? TransactionId { get; set; }            // epoint transaction (gateway)
    public string? TransactionReference { get; set; }     // bizim order_code: "MZ-{orderId}"
    public string? RedirectUrl { get; set; }              // epoint redirect url

    // Amount
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "AZN";

    // Status: pending | processing | completed | failed | refunded | cancelled
    public string Status { get; set; } = "pending";

    // Raw gateway response (json string saxlayırıq, DB-də jsonb olacaq)
    public string? GatewayResponse { get; set; }

    // Error handling
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    // Timestamps
    public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime? FailedAt { get; set; }

    // Tracking
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // Kredit (installment) seçimi
    public bool IsInstallment { get; set; } = false;
}