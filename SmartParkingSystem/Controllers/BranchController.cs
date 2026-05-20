using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartParking.DTOs.Branch;
using SmartParking.Services.Interfaces;

namespace SmartParking.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/branches")]
    public class BranchController : ControllerBase
    {
        private readonly IBranchService _service;
        private readonly IBranchAuthorizationService _branchAuthorizationService;

        public BranchController(IBranchService service, IBranchAuthorizationService branchAuthorizationService)
        {
            _service = service;
            _branchAuthorizationService = branchAuthorizationService;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(BranchCreateDto dto)
        {
            await _service.CreateAsync(dto);

            return Ok();
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var branchId = await ResolveBranchScopeAsync();
            return Ok(await _service.GetAllAsync(branchId));
        }

        [HttpGet("{id}/full")]
        public async Task<IActionResult> GetFull(Guid id)
        {
            await ResolveBranchScopeAsync(id);
            return Ok(await _service.GetFullAsync(id));
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, BranchDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            await _branchAuthorizationService.EnsureCanManageBranchAsync(id, userId, isAdmin);
            await _service.UpdateAsync(id, dto);

            return Ok();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _service.DeleteAsync(id);

            return Ok();
        }

        private async Task<Guid?> ResolveBranchScopeAsync(Guid? requestedBranchId = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedAccessException("User not found");

            return await _branchAuthorizationService.GetBranchScopeAsync(userId, User.IsInRole("Admin"), requestedBranchId);
        }
    }
}
