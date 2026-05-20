using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartParking.Models;
using SmartParking.Models.Identity;

namespace SmartParking.Data
{
    public class ApplicationDBContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options)
            : base(options)
        {
        }

        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Branch> Branches { get; set; }

        public DbSet<ParkingLot> ParkingLots { get; set; }

        public DbSet<Zone> Zones { get; set; }

        public DbSet<Slot> Slots { get; set; }
        public DbSet<Vehicle> Vehicle { get; set; }
        public DbSet<ElectronicTicket> ElectronicTickets { get; set; }
        public DbSet<CheckInOut> CheckInOuts { get; set; }
        public DbSet<VehicleLocationDetection> VehicleLocationDetections { get; set; }
        public DbSet<MonthlyPass> MonthlyPasses { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
