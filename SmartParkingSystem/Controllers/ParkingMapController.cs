using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartParking.Data;
using SmartParking.DTOs.Parking;
using SmartParking.Services.Interfaces;

namespace SmartParking.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/parking-map")]
    public class ParkingMapController : ControllerBase
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly ApplicationDBContext _context;
        private readonly IBranchAuthorizationService _branchAuthorizationService;

        public ParkingMapController(ApplicationDBContext context, IBranchAuthorizationService branchAuthorizationService)
        {
            _context = context;
            _branchAuthorizationService = branchAuthorizationService;
        }

        [HttpGet("branches/{branchId:guid}")]
        public async Task<IActionResult> Get(Guid branchId)
        {
            if (!await CanReadBranchMapAsync(branchId))
                return Forbid();

            var branch = await _context.Branches.AsNoTracking().FirstOrDefaultAsync(x => x.Id == branchId);
            if (branch is null)
                return NotFound(new { message = "Branch not found" });

            return Ok(ParseMap(branch.Id, branch.ParkingMapJson, branch.ParkingMapUpdatedAt));
        }

        [HttpPut("branches/{branchId:guid}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Save(Guid branchId, [FromBody] ParkingMapDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            await _branchAuthorizationService.EnsureCanManageBranchAsync(branchId, userId, User.IsInRole("Admin"));

            var branch = await _context.Branches.FirstOrDefaultAsync(x => x.Id == branchId);
            if (branch is null)
                return NotFound(new { message = "Branch not found" });

            dto.BranchId = branchId;
            dto.Width = dto.Width <= 0 ? 40 : dto.Width;
            dto.Height = dto.Height <= 0 ? 24 : dto.Height;
            dto.Elements ??= new List<ParkingMapElementDto>();

            branch.ParkingMapJson = JsonSerializer.Serialize(dto, JsonOptions);
            branch.ParkingMapUpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            dto.UpdatedAt = branch.ParkingMapUpdatedAt;
            return Ok(dto);
        }

        private async Task<bool> CanReadBranchMapAsync(Guid branchId)
        {
            if (User.IsInRole("Admin"))
                return true;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return false;

            if (User.IsInRole("Manager") || User.IsInRole("Staff"))
            {
                var scope = await _branchAuthorizationService.GetBranchScopeAsync(userId, false);
                return scope == branchId;
            }

            return await _context.VehicleLocationDetections
                .AsNoTracking()
                .AnyAsync(x => x.UserId == userId && x.BranchId == branchId);
        }

        private static ParkingMapDto ParseMap(Guid branchId, string? json, DateTime? updatedAt)
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                var parsed = JsonSerializer.Deserialize<ParkingMapDto>(json, JsonOptions);
                if (parsed != null)
                {
                    parsed.BranchId = branchId;
                    parsed.UpdatedAt = updatedAt;
                    return parsed;
                }
            }

            return new ParkingMapDto
            {
                BranchId = branchId,
                UpdatedAt = updatedAt
            };
        }
    }
}
