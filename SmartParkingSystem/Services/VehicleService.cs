using System;
using Microsoft.EntityFrameworkCore;
using SmartParking.Data;
using SmartParking.DTOs.Vehicle;
using SmartParking.Models;
using SmartParking.Services.Interfaces;

namespace SmartParking.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly ApplicationDBContext _context;

        public VehicleService(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task CreateVehicleAsync(string userId, CreateVehicleDto dto)
        {
            await CreateVehicleForUserAsync(userId, dto);
        }

        public async Task CreateVehicleForUserAsync(string userId, CreateVehicleDto dto)
        {
            var exists = await _context.Vehicle
                .AnyAsync(x => x.LicensePlate == dto.LicensePlate);

            if (exists)
                throw new Exception("License plate already exists");

            if (dto.IsDefault)
            {
                var oldDefaults = await _context.Vehicle
                    .Where(x => x.UserId == userId)
                    .ToListAsync();

                oldDefaults.ForEach(x => x.IsDefault = false);
            }

            var vehicle = new Vehicle
            {
                UserId = userId,
                LicensePlate = dto.LicensePlate,
                VehicleType = dto.VehicleType,
                Brand = dto.Brand,
                Color = dto.Color,
                IsDefault = dto.IsDefault
            };

            _context.Vehicle.Add(vehicle);

            await _context.SaveChangesAsync();
        }

        public async Task<List<VehicleResponseDto>> GetMyVehiclesAsync(string userId)
        {
            return await _context.Vehicle
                .Where(x => x.UserId == userId)
                .Select(x => new VehicleResponseDto
                {
                    Id = x.Id,
                    LicensePlate = x.LicensePlate,
                    UserId = x.UserId,
                    OwnerName = x.User.FullName,
                    OwnerEmail = x.User.Email,
                    VehicleType = x.VehicleType,
                    Brand = x.Brand,
                    Color = x.Color,
                    IsDefault = x.IsDefault
                })
                .ToListAsync();
        }

        public async Task<List<VehicleResponseDto>> GetAllVehiclesAsync()
        {
            return await _context.Vehicle
                .Include(x => x.User)
                .OrderBy(x => x.LicensePlate)
                .Select(x => new VehicleResponseDto
                {
                    Id = x.Id,
                    LicensePlate = x.LicensePlate,
                    UserId = x.UserId,
                    OwnerName = x.User.FullName,
                    OwnerEmail = x.User.Email,
                    VehicleType = x.VehicleType,
                    Brand = x.Brand,
                    Color = x.Color,
                    IsDefault = x.IsDefault
                })
                .ToListAsync();
        }

        public async Task UpdateVehicleAsync(Guid id, string userId, UpdateVehicleDto dto)
        {
            var vehicle = await _context.Vehicle
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            if (vehicle == null)
                throw new Exception("Vehicle not found");

            if (dto.IsDefault)
            {
                var allVehicles = await _context.Vehicle
                    .Where(x => x.UserId == userId)
                    .ToListAsync();

                allVehicles.ForEach(x => x.IsDefault = false);
            }

            vehicle.Brand = dto.Brand;
            vehicle.Color = dto.Color;
            vehicle.IsDefault = dto.IsDefault;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteVehicleAsync(Guid id, string userId)
        {
            var vehicle = await _context.Vehicle
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            if (vehicle == null)
                throw new Exception("Vehicle not found");

            await EnsureVehicleCanBeDeletedAsync(vehicle.LicensePlate);

            _context.Vehicle.Remove(vehicle);

            await _context.SaveChangesAsync();
        }

        public async Task DeleteVehicleByAdminAsync(Guid id)
        {
            var vehicle = await _context.Vehicle.FirstOrDefaultAsync(x => x.Id == id);
            if (vehicle == null)
                throw new Exception("Vehicle not found");

            await EnsureVehicleCanBeDeletedAsync(vehicle.LicensePlate);

            _context.Vehicle.Remove(vehicle);
            await _context.SaveChangesAsync();
        }

        private async Task EnsureVehicleCanBeDeletedAsync(string licensePlate)
        {
            var plate = (licensePlate ?? string.Empty).Trim().ToUpperInvariant().Replace(" ", "");
            var now = DateTime.Now;
            var hasActivePass = await _context.MonthlyPasses.AnyAsync(x =>
                x.LicensePlate.ToUpper().Replace(" ", "") == plate &&
                x.IsActive &&
                x.ValidFrom <= now &&
                x.ValidTo >= now);

            if (hasActivePass)
            {
                throw new Exception("Vehicle has an active monthly pass and cannot be deleted.");
            }
        }
    }
}
