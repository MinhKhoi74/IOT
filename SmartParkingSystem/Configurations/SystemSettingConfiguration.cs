using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartParking.Models;

namespace SmartParking.Configurations
{
    public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
    {
        public void Configure(EntityTypeBuilder<SystemSetting> builder)
        {
            builder.HasKey(x => x.Key);
            builder.Property(x => x.Key).HasMaxLength(100);
            builder.Property(x => x.Value).IsRequired().HasMaxLength(500);
            builder.Property(x => x.UpdatedAt).HasDefaultValueSql("GETDATE()");
        }
    }
}
