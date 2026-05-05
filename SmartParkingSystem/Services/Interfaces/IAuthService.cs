using SmartParking.DTOs.Auth;

namespace SmartParking.Services.Interfaces
{
    public interface IAuthService
    {
        Task<object> RegisterAsync(RegisterDto dto);
        Task<object> LoginAsync(LoginDto dto);
        Task<object> RefreshTokenAsync(RefreshTokenRequestDto dto);

        Task LogoutAsync(string refreshToken);

        Task ForgotPasswordAsync(ForgotPasswordDto dto);

        Task ResetPasswordAsync(ResetPasswordDto dto);
    }
}
