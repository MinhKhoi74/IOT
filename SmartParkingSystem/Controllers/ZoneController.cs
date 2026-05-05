using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartParking.DTOs.Branch;
using SmartParking.Services.Interfaces;

namespace SmartParking.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    [ApiController]
    [Route("api/zones")]
    public class ZoneController : ControllerBase
    {
        private readonly IZoneService _service;
        private readonly IBranchAuthorizationService _branchAuthorizationService;

        public ZoneController(IZoneService service, IBranchAuthorizationService branchAuthorizationService)
        {
            _service = service;
            _branchAuthorizationService = branchAuthorizationService;
        }

        [HttpPost]
        public async Task<IActionResult> Create(ZoneDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            await _branchAuthorizationService.EnsureCanManageParkingLotAsync(dto.ParkingLotId, userId, isAdmin);
            await _service.CreateAsync(dto);
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, ZoneDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            await _branchAuthorizationService.EnsureCanManageZoneAsync(id, userId, isAdmin);
            await _branchAuthorizationService.EnsureCanManageParkingLotAsync(dto.ParkingLotId, userId, isAdmin);
            await _service.UpdateAsync(id, dto);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            await _branchAuthorizationService.EnsureCanManageZoneAsync(id, userId, isAdmin);
            await _service.DeleteAsync(id);
            return Ok();
        }
    }
}
