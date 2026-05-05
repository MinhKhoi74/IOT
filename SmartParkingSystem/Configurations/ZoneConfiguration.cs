using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartParking.Models;

namespace SmartParking.Configurations
{
    public class ZoneConfiguration : IEntityTypeConfiguration<Zone>
    {
        public void Configure(EntityTypeBuilder<Zone> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasMany(x => x.Slots)
                .WithOne(x => x.Zone)
                .HasForeignKey(x => x.ZoneId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
