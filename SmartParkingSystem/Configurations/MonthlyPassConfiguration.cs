using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartParking.Models;

namespace SmartParking.Configurations
{
    public class MonthlyPassConfiguration : IEntityTypeConfiguration<MonthlyPass>
    {
        public void Configure(EntityTypeBuilder<MonthlyPass> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.LicensePlate)
                .IsRequired()
                .HasMaxLength(20);

            builder.HasIndex(x => x.LicensePlate)
                .IsUnique();

            builder.Property(x => x.OwnerName)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.OwnerPhone)
                .HasMaxLength(30);
        }
    }
}
