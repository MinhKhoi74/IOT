namespace SmartParking.DTOs.User
{
    public class UserListDto
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string[] Roles { get; set; }
        public bool IsActive { get; set; }
        public BranchInfoDto? Branch { get; set; }
    }
}
