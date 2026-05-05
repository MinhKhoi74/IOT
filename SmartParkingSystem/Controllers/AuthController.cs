using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartParking.DTOs.Auth;
using SmartParking.Services;
using SmartParking.Services.Interfaces;

namespace SmartParking.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
            => Ok(await _authService.RegisterAsync(dto));

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
            => Ok(await _authService.LoginAsync(dto));

        [HttpPost("refresh-token")]
        public async Task<IActionResult> Refresh(RefreshTokenRequestDto dto)
            => Ok(await _authService.RefreshTokenAsync(dto));
    }
}
