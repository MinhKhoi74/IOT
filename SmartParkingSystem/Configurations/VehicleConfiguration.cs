using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartParking.Models;

namespace SmartParking.Configurations
{
    public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
    {
        public void Configure(EntityTypeBuilder<Vehicle> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.LicensePlate)
                .IsRequired()
                .HasMaxLength(20);

            builder.HasIndex(x => x.LicensePlate)
                .IsUnique();

            builder.Property(x => x.Brand)
                .HasMaxLength(100);

            builder.Property(x => x.Color)
                .HasMaxLength(50);

            builder.HasOne(x => x.User)
                .WithMany(x => x.Vehicles)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
