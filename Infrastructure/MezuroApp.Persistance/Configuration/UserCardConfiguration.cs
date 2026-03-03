using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MezuroApp.Domain.Entities;

public sealed class UserCardConfiguration : IEntityTypeConfiguration<UserCard>
{
    public void Configure(EntityTypeBuilder<UserCard> b)
    {

        b.HasIndex(x => new { x.UserId, x.IsDefault });
        b.Property(x => x.CardUid).HasMaxLength(255);
        b.Property(x => x.CardName).HasMaxLength(255);
        b.Property(x => x.CardMask).HasMaxLength(255);
        b.Property(x => x.CardExpiry).HasMaxLength(255);
        b.Property(x => x.OperationCode).HasMaxLength(255);
        b.Property(x => x.Rrn).HasMaxLength(255);
    }
}