namespace SmartParking.DTOs.User
{
    public class UserDetailDto
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string[] Roles { get; set; }
        public bool IsActive { get; set; }
        public BranchInfoDto Branch { get; set; }
    }

    public class BranchInfoDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
    }
}
