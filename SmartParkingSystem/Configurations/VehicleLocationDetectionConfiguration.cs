using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartParking.Models;

namespace SmartParking.Configurations
{
    public class VehicleLocationDetectionConfiguration : IEntityTypeConfiguration<VehicleLocationDetection>
    {
        public void Configure(EntityTypeBuilder<VehicleLocationDetection> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.LicensePlate)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.CameraId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.ParkingLotCode)
                .HasMaxLength(100);

            builder.Property(x => x.ZoneCode)
                .HasMaxLength(100);

            builder.Property(x => x.ColumnCode)
                .HasMaxLength(100);

            builder.Property(x => x.LocationName)
                .IsRequired()
                .HasMaxLength(250);

            builder.Property(x => x.Status)
                .IsRequired()
                .HasMaxLength(40);

            builder.Property(x => x.Severity)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(x => x.Message)
                .HasMaxLength(500);

            builder.HasIndex(x => x.LicensePlate)
                .IsUnique()
                .HasDatabaseName("IX_VehicleLocation_UniquePlate");

            builder.HasIndex(x => x.DetectedAt)
                .HasDatabaseName("IX_VehicleLocation_DetectedAt");

            builder.HasIndex(x => x.Status)
                .HasDatabaseName("IX_VehicleLocation_Status");

            builder.HasOne(x => x.Vehicle)
                .WithMany()
                .HasForeignKey(x => x.VehicleId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.CheckInOut)
                .WithMany()
                .HasForeignKey(x => x.CheckInOutId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
