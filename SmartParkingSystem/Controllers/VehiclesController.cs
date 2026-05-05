using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartParking.DTOs.Vehicle;
using SmartParking.Services.Interfaces;

namespace SmartParking.Controllers
{
    [ApiController]
    [Route("api/vehicles")]
    [Authorize]
    public class VehiclesController : ControllerBase
    {
        private readonly IVehicleService _vehicleService;

        public VehiclesController(IVehicleService vehicleService)
        {
            _vehicleService = vehicleService;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateVehicleDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            await _vehicleService.CreateVehicleAsync(userId, dto);

            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetMyVehicles()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var result = await _vehicleService.GetMyVehiclesAsync(userId);

            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdateVehicleDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            await _vehicleService.UpdateVehicleAsync(id, userId, dto);

            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            await _vehicleService.DeleteVehicleAsync(id, userId);

            return Ok();
        }
    }
}
