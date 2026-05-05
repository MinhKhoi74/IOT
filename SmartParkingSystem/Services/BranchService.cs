using Microsoft.EntityFrameworkCore;
using SmartParking.Data;
using SmartParking.DTOs.Branch;
using SmartParking.Models;
using SmartParking.Services.Interfaces;

namespace SmartParking.Services
{
    public class BranchService : IBranchService
    {
        private readonly ApplicationDBContext _context;

        public BranchService(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(BranchCreateDto dto)
        {
            var branch = new Branch
            {
                Name = dto.Name,
                Address = dto.Address
            };

            _context.Branches.Add(branch);

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<BranchDto>> GetAllAsync()
        {
            return await _context.Branches
                .Select(b => new BranchDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Address = b.Address
                })
                .ToListAsync();
        }
        public async Task<BranchFullDto?> GetFullAsync(Guid id)
        {
            return await _context.Branches
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(b => new BranchFullDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Address = b.Address,
                    ParkingLots = b.ParkingLots.Select(p => new ParkingLotDetailDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Zones = p.Zones.Select(z => new ZoneDetailDto
                        {
                            Id = z.Id,
                            Name = z.Name,
                            VehicleType = z.VehicleType.ToString(), // Convert Enum sang String
                                                                    // Map từ Slot Entity sang SlotDetailDto
                            Slots = z.Slots.Select(s => new SlotDetailDto
                            {
                                Id = s.Id,
                                SlotCode = s.SlotCode,
                                Status = s.Status.ToString() // Convert Enum sang String
                            }).ToList()
                        }).ToList()
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(Guid id, BranchDto dto)
        {
            var branch = await _context.Branches.FindAsync(id);

            if (branch == null)
                throw new Exception("Branch not found");

            branch.Name = dto.Name;
            branch.Address = dto.Address;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var branch = await _context.Branches.FindAsync(id);

            if (branch == null)
                throw new Exception("Branch not found");

            _context.Branches.Remove(branch);

            await _context.SaveChangesAsync();
        }

    }
}
