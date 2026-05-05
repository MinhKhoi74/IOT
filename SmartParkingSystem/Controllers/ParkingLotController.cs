using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartParking.DTOs.Branch;
using SmartParking.Services.Interfaces;

namespace SmartParking.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    [ApiController]
    [Route("api/parkinglots")]
    public class ParkingLotController : ControllerBase
    {
        private readonly IParkingLotService _service;
        private readonly IBranchAuthorizationService _branchAuthorizationService;

        public ParkingLotController(IParkingLotService service, IBranchAuthorizationService branchAuthorizationService)
        {
            _service = service;
            _branchAuthorizationService = branchAuthorizationService;
        }

        [HttpPost]
        public async Task<IActionResult> Create(ParkingLotDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            await _branchAuthorizationService.EnsureCanManageBranchAsync(dto.BranchId, userId, isAdmin);
            await _service.CreateAsync(dto);
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        //[HttpGet("{id}/available-slots")]
        //public async Task<IActionResult> GetAvailableSlots(Guid id)
        //{
        //    return Ok(await _service.GetAvailableSlotsAsync(id));
        //}

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, ParkingLotDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            await _branchAuthorizationService.EnsureCanManageParkingLotAsync(id, userId, isAdmin);
            await _branchAuthorizationService.EnsureCanManageBranchAsync(dto.BranchId, userId, isAdmin);
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

            await _branchAuthorizationService.EnsureCanManageParkingLotAsync(id, userId, isAdmin);
            await _service.DeleteAsync(id);
            return Ok();
        }
    }
}
