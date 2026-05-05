using System;
using Microsoft.EntityFrameworkCore;
using SmartParking.Data;
using SmartParking.Services.Interfaces;

namespace SmartParking.Services
{
    public class VehicleAuthorizationService : IVehicleAuthorizationService
    {
        private readonly ApplicationDBContext _context;

        public VehicleAuthorizationService(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task ValidateVehicleOwnership(Guid vehicleId, string userId)
        {
            var exists = await _context.Vehicle
                .AnyAsync(x =>
                    x.Id == vehicleId &&
                    x.UserId == userId);

            if (!exists)
                throw new UnauthorizedAccessException("Vehicle access denied");
        }
    }
}
