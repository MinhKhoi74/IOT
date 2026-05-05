using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartParking.DTOs.Branch;
using SmartParking.Services.Interfaces;

namespace SmartParking.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/slots")]
    public class SlotController : ControllerBase
    {
        private readonly ISlotService _service;
        private readonly IBranchAuthorizationService _branchAuthorizationService;

        public SlotController(ISlotService service, IBranchAuthorizationService branchAuthorizationService)
        {
            _service = service;
            _branchAuthorizationService = branchAuthorizationService;
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        public async Task<IActionResult> Create(SlotDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            await _branchAuthorizationService.EnsureCanManageZoneAsync(dto.ZoneId, userId, isAdmin);
            await _service.CreateAsync(dto);
            return Ok();
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        //[HttpGet("by-branch/{branchId}")]
        //public async Task<IActionResult> GetByBranch(Guid branchId)
        //{
        //    return Ok(await _service.GetByBranchAsync(branchId));
        //}

        [Authorize(Roles = "Admin,Manager")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, SlotDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            await _branchAuthorizationService.EnsureCanManageSlotAsync(id, userId, isAdmin);
            await _branchAuthorizationService.EnsureCanManageZoneAsync(dto.ZoneId, userId, isAdmin);
            await _service.UpdateAsync(id, dto);
            return Ok();
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            await _branchAuthorizationService.EnsureCanManageSlotAsync(id, userId, isAdmin);
            await _service.DeleteAsync(id);
            return Ok();
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, UpdateSlotStatusDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            await _branchAuthorizationService.EnsureCanManageSlotAsync(id, userId, isAdmin);
            await _service.UpdateStatusAsync(id, dto);
            return Ok();
        }
    }
}
