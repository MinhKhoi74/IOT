using SmartParking.DTOs.User;

namespace SmartParking.Services.Interfaces
{
    public interface IUserService
    {
        Task ChangePasswordAsync(string userId, ChangePasswordDto dto);

        Task<UserProfileDto> GetProfileAsync(string userId);

        Task UpdateProfileAsync(string userId, UpdateProfileDto dto);

        Task<List<UserListDto>> GetAllUsersAsync();

        Task<UserDetailDto> GetUserDetailAsync(string userId, string currentUserId);

        Task<string> CreateCustomerAsync(CreateCustomerDto dto);

        Task<string> CreateManagerAsync(CreateManagerDto dto);

        Task<string> CreateStaffAsync(CreateStaffDto dto);

        Task DeleteUserAsync(string userId);

        Task<List<UserListDto>> GetStaffAsync(string requesterId);

        Task<string> CreateStaffByManagerAsync(string managerId, CreateStaffDto dto);

        Task DeleteStaffAsync(string managerId, string staffId);
    }
}

