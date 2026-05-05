using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartParking.Models;

namespace SmartParking.Configurations
{
    public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
    {
        public void Configure(EntityTypeBuilder<Wallet> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Balance)
                .HasPrecision(18, 2)
                .HasDefaultValue(0m);

            builder.HasIndex(x => x.UserId)
                .IsUnique();

            builder.HasOne(x => x.User)
                .WithOne(x => x.Wallet)
                .HasForeignKey<Wallet>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
