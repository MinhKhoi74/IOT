using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartParking.Models;

namespace SmartParking.Configurations
{
    public class ParkingLotConfiguration : IEntityTypeConfiguration<ParkingLot>
    {
        public void Configure(EntityTypeBuilder<ParkingLot> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasMany(x => x.Zones)
                .WithOne(x => x.ParkingLot)
                .HasForeignKey(x => x.ParkingLotId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
