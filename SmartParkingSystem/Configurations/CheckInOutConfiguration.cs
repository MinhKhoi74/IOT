using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartParking.Models;

namespace SmartParking.Configurations
{
    public class CheckInOutConfiguration : IEntityTypeConfiguration<CheckInOut>
    {
        public void Configure(EntityTypeBuilder<CheckInOut> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.LicensePlate)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(c => c.PlateImagePath)
                .HasMaxLength(500);

            builder.Property(c => c.CheckInStationId)
                .HasMaxLength(50);

            builder.Property(c => c.CheckOutStationId)
                .HasMaxLength(50);

            builder.Property(c => c.FeeStatus)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");

            builder.Property(c => c.PaymentStatus)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");

            builder.Property(c => c.PaymentMethod)
                .HasMaxLength(20);

            builder.Property(c => c.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");

            builder.Property(c => c.FeeAmount)
                .HasPrecision(10, 2);

            // Indexes for performance
            builder.HasIndex(c => new { c.LicensePlate, c.Status })
                .HasName("IX_LicensePlate_Active")
                .HasFilter("[Status] = 'Active'");

            builder.HasIndex(c => c.CheckInTime)
                .HasName("IX_CheckInTime");

            builder.HasIndex(c => c.FeeStatus)
                .HasName("IX_FeeStatus");

            builder.HasIndex(c => c.PaymentStatus)
                .HasDatabaseName("IX_CheckInOut_PaymentStatus");

            builder.HasOne(c => c.Vehicle)
                .WithMany()
                .HasForeignKey(c => c.VehicleId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(c => c.WalletTransaction)
                .WithMany(t => t.ParkingSessions)
                .HasForeignKey(c => c.WalletTransactionId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
