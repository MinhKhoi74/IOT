namespace SmartParking.DTOs.Auth
{
    public class AuthResponseDto
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public UserInfoDto User { get; set; }
    }

    public class UserInfoDto
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string[] Roles { get; set; }
    }
}
