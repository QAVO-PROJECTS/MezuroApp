using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MezuroApp.Domain.Entities;

namespace MezuroApp.Persistance.Configuration;

public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> b)
    {
        b.ToTable("payment_transactions");

        b.HasKey(x => x.Id);

        b.Property(x => x.PaymentMethod).HasMaxLength(50).IsRequired();
        b.Property(x => x.TransactionId).HasMaxLength(255);
        b.Property(x => x.TransactionReference).HasMaxLength(255);
        b.Property(x => x.Currency).HasMaxLength(3).HasDefaultValue("AZN");
        b.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("pending");

        // PostgreSQL jsonb
        b.Property(x => x.GatewayResponse).HasColumnType("jsonb");

        b.Property(x => x.IpAddress).HasMaxLength(50);

        b.HasOne(x => x.Order)
            .WithMany(o => o.PaymentTransactions)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => x.OrderId);
        b.HasIndex(x => x.TransactionId);
        b.HasIndex(x => x.Status);
    }
}