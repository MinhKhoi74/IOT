using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartParking.Models;

namespace SmartParking.Configurations
{
    public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
    {
        public void Configure(EntityTypeBuilder<WalletTransaction> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Type)
                .IsRequired()
                .HasMaxLength(30);

            builder.Property(x => x.Amount)
                .HasPrecision(18, 2);

            builder.Property(x => x.BalanceBefore)
                .HasPrecision(18, 2);

            builder.Property(x => x.BalanceAfter)
                .HasPrecision(18, 2);

            builder.Property(x => x.ReferenceType)
                .HasMaxLength(50);

            builder.Property(x => x.ReferenceId)
                .HasMaxLength(100);

            builder.Property(x => x.Description)
                .HasMaxLength(255);

            builder.HasIndex(x => new { x.WalletId, x.CreatedAt })
                .HasDatabaseName("IX_WalletTransaction_Wallet_CreatedAt");

            builder.HasOne(x => x.Wallet)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.WalletId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
