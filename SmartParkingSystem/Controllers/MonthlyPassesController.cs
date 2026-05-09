using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SmartParking.DTOs;
using SmartParking.Services.Interfaces;

namespace SmartParking.Controllers
{
    [ApiController]
    [Route("api/monthly-passes")]
    [Authorize]
    public class MonthlyPassesController : ControllerBase
    {
        private readonly IMonthlyPassService _monthlyPassService;

        public MonthlyPassesController(IMonthlyPassService monthlyPassService)
        {
            _monthlyPassService = monthlyPassService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _monthlyPassService.GetAllAsync());
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Upsert([FromBody] MonthlyPassUpsertRequest request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                return Ok(await _monthlyPassService.UpsertAsync(request));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterForCurrentUser([FromBody] MonthlyPassUpsertRequest request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized();
                }

                return Ok(await _monthlyPassService.RegisterForUserAsync(userId, request));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            await _monthlyPassService.DeleteAsync(id);
            return NoContent();
        }
    }
}
