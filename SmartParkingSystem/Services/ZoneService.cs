using Microsoft.EntityFrameworkCore;
using SmartParking.Data;
using SmartParking.DTOs.Branch;
using SmartParking.Models;
using SmartParking.Services.Interfaces;

namespace SmartParking.Services
{
    public class ZoneService : IZoneService
    {
        private readonly ApplicationDBContext _context;

        public ZoneService(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(ZoneDto dto)
        {
            var parkingLot = await _context.ParkingLots.FindAsync(dto.ParkingLotId);

            if (parkingLot == null)
                throw new Exception("ParkingLot not found");

            var zone = new Zone
            {
                Name = dto.Name,
                VehicleType = dto.VehicleType,
                ParkingLotId = dto.ParkingLotId
            };

            _context.Zones.Add(zone);

            await _context.SaveChangesAsync();
        }

        public async Task<List<Zone>> GetAllAsync()
        {
            return await _context.Zones
                .Include(x => x.ParkingLot)
                .ToListAsync();
        }

        public async Task UpdateAsync(Guid id, ZoneDto dto)
        {
            var zone = await _context.Zones.FindAsync(id);

            if (zone == null)
                throw new Exception("Zone not found");

            zone.Name = dto.Name;
            zone.VehicleType = dto.VehicleType;
            zone.ParkingLotId = dto.ParkingLotId;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var zone = await _context.Zones.FindAsync(id);

            if (zone == null)
                throw new Exception("Zone not found");

            _context.Zones.Remove(zone);

            await _context.SaveChangesAsync();
        }
    }
}
