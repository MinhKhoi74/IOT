using Microsoft.EntityFrameworkCore;
using SmartParking.Data;
using SmartParking.Services.Interfaces;

namespace SmartParking.Services
{
    public class BranchAuthorizationService : IBranchAuthorizationService
    {
        private readonly ApplicationDBContext _context;

        public BranchAuthorizationService(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task EnsureCanManageBranchAsync(Guid branchId, string userId, bool isAdmin)
        {
            if (isAdmin)
                return;

            var hasAccess = await _context.Branches.AnyAsync(x => x.Id == branchId && x.ManagerId == userId);

            if (!hasAccess)
                throw new UnauthorizedAccessException("Branch access denied");
        }

        public async Task EnsureCanManageParkingLotAsync(Guid parkingLotId, string userId, bool isAdmin)
        {
            if (isAdmin)
                return;

            var hasAccess = await _context.ParkingLots
                .Include(x => x.Branch)
                .AnyAsync(x => x.Id == parkingLotId && x.Branch.ManagerId == userId);

            if (!hasAccess)
                throw new UnauthorizedAccessException("Parking lot access denied");
        }

        public async Task EnsureCanManageZoneAsync(Guid zoneId, string userId, bool isAdmin)
        {
            if (isAdmin)
                return;

            var hasAccess = await _context.Zones
                .Include(x => x.ParkingLot)
                .ThenInclude(x => x.Branch)
                .AnyAsync(x => x.Id == zoneId && x.ParkingLot.Branch.ManagerId == userId);

            if (!hasAccess)
                throw new UnauthorizedAccessException("Zone access denied");
        }

        public async Task EnsureCanManageSlotAsync(Guid slotId, string userId, bool isAdmin)
        {
            if (isAdmin)
                return;

            var hasAccess = await _context.Slots
                .Include(x => x.Zone)
                .ThenInclude(x => x.ParkingLot)
                .ThenInclude(x => x.Branch)
                .AnyAsync(x => x.Id == slotId && x.Zone.ParkingLot.Branch.ManagerId == userId);

            if (!hasAccess)
                throw new UnauthorizedAccessException("Slot access denied");
        }
    }
}
