using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartParking.Configurations;
using SmartParking.Data;
using SmartParking.DTOs.Auth;
using SmartParking.Helpers;
using SmartParking.Models.Identity;
using SmartParking.Services.Interfaces;

namespace SmartParking.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;
        private readonly ApplicationDBContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWalletService _walletService;

        public AuthService(UserManager<ApplicationUser> userManager, IConfiguration config, ApplicationDBContext context, RoleManager<IdentityRole> roleManager, IWalletService walletService)
        {
            _userManager = userManager;
            _config = config;
            _context = context;
            _roleManager = roleManager;
            _walletService = walletService;
        }

        public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user == null) return;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // gửi email token sau
        }

        public async Task<object> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
                throw new Exception("Invalid email or password");
            var roles = await _userManager.GetRolesAsync(user);
            var jwt = JwtHelper.GenerateJwtToken(user, roles, _config.GetSection("JwtSettings").Get<JwtSettings>());
            var refreshToken = new RefreshToken
            {
                Token = Guid.NewGuid().ToString(),
                Expires = DateTime.UtcNow.AddDays(7),
                UserId = user.Id
            };
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();
            return new
            {
                Token = jwt,
                RefreshToken = refreshToken.Token,
                User = new
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    Roles = roles.ToArray()
                }
            };
        }

        public async Task LogoutAsync(string refreshToken)
        {
            var token = await _context.RefreshTokens.FirstOrDefaultAsync(x => x.Token == refreshToken);

            if (token != null)
            {
                token.IsRevoked = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<object> RefreshTokenAsync(RefreshTokenRequestDto dto)
        {
            var token = await _context.RefreshTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Token == dto.RefreshToken && !x.IsRevoked);

            if (token == null || token.Expires < DateTime.UtcNow)
                throw new Exception("Invalid refresh token");

            var roles = await _userManager.GetRolesAsync(token.User);

            var jwt = JwtHelper.GenerateJwtToken(token.User, roles, _config.GetSection("JwtSettings").Get<JwtSettings>());

            return new { AccessToken = jwt };
        }


        public async Task<object> RegisterAsync(RegisterDto dto)
        {
            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
            };
            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
            await _userManager.AddToRoleAsync(user, "Customer");
            await _walletService.EnsureWalletAsync(user.Id);
            
            // Generate token and refresh token
            var roles = await _userManager.GetRolesAsync(user);
            var jwt = JwtHelper.GenerateJwtToken(user, roles, _config.GetSection("JwtSettings").Get<JwtSettings>());
            var refreshToken = new RefreshToken
            {
                Token = Guid.NewGuid().ToString(),
                Expires = DateTime.UtcNow.AddDays(7),
                UserId = user.Id
            };
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();
            
            return new
            {
                Token = jwt,
                RefreshToken = refreshToken.Token,
                User = new
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    Roles = roles.ToArray()
                }
            };
        }

        public async Task ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user == null)
                throw new Exception("User not found");

            var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);

            if (!result.Succeeded)
                throw new Exception("Reset password failed");
        }
    }
}
