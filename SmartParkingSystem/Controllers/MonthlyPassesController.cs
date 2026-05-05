using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartParking.DTOs;
using SmartParking.Services.Interfaces;

namespace SmartParking.Controllers
{
    [ApiController]
    [Route("api/monthly-passes")]
    [Authorize(Roles = "Admin")]
    public class MonthlyPassesController : ControllerBase
    {
        private readonly IMonthlyPassService _monthlyPassService;

        public MonthlyPassesController(IMonthlyPassService monthlyPassService)
        {
            _monthlyPassService = monthlyPassService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _monthlyPassService.GetAllAsync());
        }

        [HttpPost]
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

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _monthlyPassService.DeleteAsync(id);
            return NoContent();
        }
    }
}
