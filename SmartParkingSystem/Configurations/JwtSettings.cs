namespace SmartParking.Configurations
{
    public class JwtSettings
    {
        public string Secret { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public int AccessTokenMinutes { get; set; }
        public int RefreshTokenDays { get; set; }
    }
}
