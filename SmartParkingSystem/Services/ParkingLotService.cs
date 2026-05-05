using Microsoft.EntityFrameworkCore;
using SmartParking.Data;
using SmartParking.DTOs.Branch;
using SmartParking.Models;
using SmartParking.Models.Enums;
using SmartParking.Services.Interfaces;

namespace SmartParking.Services
{
    public class ParkingLotService : IParkingLotService
    {
        private readonly ApplicationDBContext _context;

        public ParkingLotService(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(ParkingLotDto dto)
        {
            var branch = await _context.Branches.FindAsync(dto.BranchId);

            if (branch == null)
                throw new Exception("Branch not found");

            var parkingLot = new ParkingLot
            {
                Name = dto.Name,
                BranchId = dto.BranchId
            };

            _context.ParkingLots.Add(parkingLot);

            await _context.SaveChangesAsync();
        }

        public async Task<List<ParkingLot>> GetAllAsync()
        {
            return await _context.ParkingLots
                .Include(x => x.Branch)
                .ToListAsync();
        }
        public async Task<AvailableSlotResponseDto> GetAvailableSlotsAsync(Guid parkingLotId)
        {
            var slots = await _context.Slots
                .Include(x => x.Zone)
                .Where(x =>
                    x.Zone.ParkingLotId == parkingLotId &&
                    x.Status == SlotStatus.Available)
                .Select(x => new AvailableSlotDto
                {
                    Id = x.Id,
                    SlotCode = x.SlotCode,
                    ZoneName = x.Zone.Name
                })
                .ToListAsync();

            return new AvailableSlotResponseDto
            {
                AvailableCount = slots.Count,
                Slots = slots
            };
        }
        public async Task UpdateAsync(Guid id, ParkingLotDto dto)
        {
            var parkingLot = await _context.ParkingLots.FindAsync(id);

            if (parkingLot == null)
                throw new Exception("ParkingLot not found");

            parkingLot.Name = dto.Name;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var parkingLot = await _context.ParkingLots.FindAsync(id);

            if (parkingLot == null)
                throw new Exception("ParkingLot not found");

            _context.ParkingLots.Remove(parkingLot);

            await _context.SaveChangesAsync();
        }
    }
}
