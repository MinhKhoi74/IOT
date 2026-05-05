using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartParking.DTOs.User;
using SmartParking.Services.Interfaces;

namespace SmartParking.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return Ok(await _userService.GetProfileAsync(userId));
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            await _userService.UpdateProfileAsync(userId, dto);

            return Ok(new { message = "Profile updated successfully" });
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            await _userService.ChangePasswordAsync(userId, dto);

            return Ok();
        }

        [HttpGet("list")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{userId}/detail")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserDetail(string userId)
        {
            try
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userDetail = await _userService.GetUserDetailAsync(userId, currentUserId);
                return Ok(userDetail);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("create-customer")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCustomer(CreateCustomerDto dto)
        {
            try
            {
                var userId = await _userService.CreateCustomerAsync(dto);
                return Ok(new { message = "Customer created successfully", userId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("create-manager")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateManager(CreateManagerDto dto)
        {
            try
            {
                var userId = await _userService.CreateManagerAsync(dto);
                return Ok(new { message = "Manager created successfully", userId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            try
            {
                await _userService.DeleteUserAsync(userId);
                return Ok(new { message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ========== Manager Endpoints ==========
        [HttpGet("staff-list")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> GetStaffInMyBranch()
        {
            try
            {
                var managerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var staff = await _userService.GetStaffAsync(managerId);
                return Ok(staff);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("create-staff")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateStaff(CreateStaffDto dto)
        {
            try
            {
                var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var staffId = await _userService.CreateStaffByManagerAsync(adminId, dto);
                return Ok(new { message = "Staff created successfully", staffId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("staff/create")]
        [Authorize(Roles = "Manager, Admin")]
        public async Task<IActionResult> CreateStaffByManager(CreateStaffDto dto)
        {
            try
            {
                var managerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var staffId = await _userService.CreateStaffByManagerAsync(managerId, dto);
                return Ok(new { message = "Staff created successfully", staffId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("staff/{staffId}")]
        [Authorize(Roles = "Manager, Admin")]
        public async Task<IActionResult> DeleteStaff(string staffId)
        {
            try
            {
                var managerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _userService.DeleteStaffAsync(managerId, staffId);
                return Ok(new { message = "Staff deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
