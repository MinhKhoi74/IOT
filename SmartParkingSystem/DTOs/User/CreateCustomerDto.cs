namespace SmartParking.DTOs.User
{
    public class CreateCustomerDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
