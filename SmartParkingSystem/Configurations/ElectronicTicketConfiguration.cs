using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartParking.Models;

namespace SmartParking.Configurations
{
    public class ElectronicTicketConfiguration : IEntityTypeConfiguration<ElectronicTicket>
    {
        public void Configure(EntityTypeBuilder<ElectronicTicket> builder)
        {
            builder.ToTable("ElectronicTickets");

            builder.HasKey(x => x.Id);

            // Required properties
            builder.Property(x => x.LicensePlate)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(x => x.TicketCode)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.CheckInDateTime)
                .IsRequired();

            builder.Property(x => x.ParkingLotName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(x => x.BranchName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(x => x.Status)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.AttemptCount)
                .IsRequired()
                .HasDefaultValue(0);

            // Optional properties
            builder.Property(x => x.CheckOutDateTime)
                .IsRequired(false);

            builder.Property(x => x.UserId)
                .IsRequired(false)
                .HasMaxLength(450);

            builder.Property(x => x.FeeAmount)
                .IsRequired(false)
                .HasPrecision(10, 2);

            builder.Property(x => x.PaymentMethod)
                .IsRequired(false);

            builder.Property(x => x.SentToUserAt)
                .IsRequired(false);

            // Indexes for performance
            builder.HasIndex(x => x.LicensePlate)
                .HasName("IX_ElectronicTicket_LicensePlate");

            builder.HasIndex(x => x.TicketCode)
                .HasName("IX_ElectronicTicket_TicketCode")
                .IsUnique();

            builder.HasIndex(x => x.UserId)
                .HasName("IX_ElectronicTicket_UserId");

            builder.HasIndex(x => x.Status)
                .HasName("IX_ElectronicTicket_Status");

            builder.HasIndex(x => x.CheckInDateTime)
                .HasName("IX_ElectronicTicket_CheckInDateTime");

            builder.HasIndex(x => x.CreatedAt)
                .HasName("IX_ElectronicTicket_CreatedAt");

            // Relationship with ApplicationUser
            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_ElectronicTicket_User");
        }
    }
}
