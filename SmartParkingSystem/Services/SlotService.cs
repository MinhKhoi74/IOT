using Microsoft.EntityFrameworkCore;
using SmartParking.Data;
using SmartParking.DTOs.Branch;
using SmartParking.Models;
using SmartParking.Models.Enums;
using SmartParking.Services.Interfaces;

namespace SmartParking.Services
{
    public class SlotService : ISlotService
    {
        private readonly ApplicationDBContext _context;

        public SlotService(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(SlotDto dto)
        {
            if (await _context.Slots.AnyAsync(x => x.SlotCode == dto.SlotCode))
                throw new Exception("SlotCode already exists");

            var zone = await _context.Zones.FindAsync(dto.ZoneId);

            if (zone == null)
                throw new Exception("Zone not found");

            var slot = new Slot
            {
                SlotCode = dto.SlotCode,
                ZoneId = dto.ZoneId,
                Status = SlotStatus.Available
            };

            _context.Slots.Add(slot);

            await _context.SaveChangesAsync();
        }


        public async Task<List<Slot>> GetAllAsync()
        {
            return await _context.Slots
                .Include(x => x.Zone)
                .ToListAsync();
        }

        public async Task<List<Slot>> GetByBranchAsync(Guid branchId)
        {
            return await _context.Slots
                .Include(x => x.Zone)
                .ThenInclude(x => x.ParkingLot)
                .Where(x => x.Zone.ParkingLot.BranchId == branchId)
                .ToListAsync();
        }

        public async Task UpdateAsync(Guid id, SlotDto dto)
        {
            var slot = await _context.Slots.FindAsync(id);

            if (slot == null)
                throw new Exception("Slot not found");

            slot.SlotCode = dto.SlotCode;
            slot.ZoneId = dto.ZoneId;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var slot = await _context.Slots.FindAsync(id);

            if (slot == null)
                throw new Exception("Slot not found");

            _context.Slots.Remove(slot);

            await _context.SaveChangesAsync();
        }
        public async Task UpdateStatusAsync(Guid id, UpdateSlotStatusDto dto)
        {
            var slot = await _context.Slots.FindAsync(id);

            if (slot == null)
                throw new Exception("Slot not found");

            slot.Status = dto.Status;

            await _context.SaveChangesAsync();
        }
    }
}
