using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartParking.Data;
using SmartParking.DTOs.Parking;
using SmartParking.DTOs.User;
using SmartParking.DTOs.Vehicle;
using SmartParking.Models.Identity;
using SmartParking.Services.Interfaces;

namespace SmartParking.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDBContext _context;

        public UserService(UserManager<ApplicationUser> userManager, ApplicationDBContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task ChangePasswordAsync(string userId, ChangePasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new Exception("User not found");

            var result = await _userManager.ChangePasswordAsync(
                user,
                dto.CurrentPassword,
                dto.NewPassword);

            if (!result.Succeeded)
            {
                // Lấy lỗi đầu tiên mà Identity trả về để biết nguyên nhân
                var error = result.Errors.FirstOrDefault()?.Description ?? "Change password failed";
                throw new Exception(error);
            }
        }

        public async Task<UserProfileDto> GetProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found");

            var roles = await _userManager.GetRolesAsync(user);
            var wallet = await _context.Wallets.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);
            var branch = user.BranchId.HasValue
                ? await _context.Branches.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.BranchId)
                : null;

            var vehicles = await _context.Vehicle
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.IsActive)
                .Select(x => new VehicleResponseDto
                {
                    Id = x.Id,
                    LicensePlate = x.LicensePlate,
                    VehicleType = x.VehicleType,
                    Brand = x.Brand,
                    Color = x.Color,
                    IsDefault = x.IsDefault
                })
                .ToListAsync();

            var plates = vehicles.Select(x => x.LicensePlate).ToList();
            var monthlyPasses = await _context.MonthlyPasses
                .AsNoTracking()
                .Where(x => plates.Contains(x.LicensePlate))
                .OrderByDescending(x => x.ValidTo)
                .Select(x => new MonthlyPassProfileDto
                {
                    Id = x.Id,
                    LicensePlate = x.LicensePlate,
                    OwnerName = x.OwnerName,
                    OwnerPhone = x.OwnerPhone,
                    ValidFrom = x.ValidFrom,
                    ValidTo = x.ValidTo,
                    IsActive = x.IsActive
                })
                .ToListAsync();

            var recentHistory = await _context.CheckInOuts
                .AsNoTracking()
                .Where(x => x.UserId == userId || plates.Contains(x.LicensePlate))
                .OrderByDescending(x => x.CheckInTime)
                .Take(20)
                .Select(x => new ParkingHistoryItemDto
                {
                    Id = x.Id,
                    LicensePlate = x.LicensePlate,
                    CheckInTime = x.CheckInTime,
                    CheckOutTime = x.CheckOutTime,
                    DurationMinutes = x.DurationMinutes,
                    FeeAmount = x.FeeAmount,
                    FeeStatus = x.FeeStatus,
                    PaymentStatus = x.PaymentStatus,
                    PaymentMethod = x.PaymentMethod,
                    Status = x.Status,
                    CheckInStationId = x.CheckInStationId,
                    CheckInImageBase64 = x.CheckInImageBase64,
                    CheckOutStationId = x.CheckOutStationId,
                    CheckOutImageBase64 = x.CheckOutImageBase64,
                    VehicleId = x.VehicleId,
                    UserId = x.UserId
                })
                .ToListAsync();

            return new UserProfileDto
            {
                Id = user.Id,
                UserName = user.UserName,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Roles = roles.ToArray(),
                IsActive = user.IsActive,
                Branch = branch == null
                    ? null
                    : new BranchInfoDto
                    {
                        Id = branch.Id,
                        Name = branch.Name,
                        Address = branch.Address
                    },
                Wallet = new WalletInfoDto
                {
                    Id = wallet?.Id,
                    Balance = wallet?.Balance ?? 0m,
                    CreatedAt = wallet?.CreatedAt,
                    UpdatedAt = wallet?.UpdatedAt
                },
                Vehicles = vehicles,
                MonthlyPasses = monthlyPasses,
                RecentParkingHistory = recentHistory
            };
        }

        public async Task UpdateProfileAsync(string userId, UpdateProfileDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);

            user.FullName = dto.FullName;
            user.PhoneNumber = dto.PhoneNumber;

            await _userManager.UpdateAsync(user);
        }

        public async Task<List<UserListDto>> GetAllUsersAsync(string requesterId)
        {
            var requester = await _userManager.FindByIdAsync(requesterId);
            if (requester == null)
                throw new Exception("User not found");

            var requesterRoles = await _userManager.GetRolesAsync(requester);
            var isAdmin = requesterRoles.Contains("Admin");
            var isManager = requesterRoles.Contains("Manager");
            if (!isAdmin && !isManager)
                throw new Exception("You don't have permission to view users");

            if (isManager && !requester.BranchId.HasValue)
                throw new Exception("Manager has no assigned branch");

            var query = _userManager.Users.AsQueryable();
            if (isManager)
            {
                query = query.Where(x => x.BranchId == requester.BranchId);
            }

            var users = await query.ToListAsync();
            var result = new List<UserListDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var branch = user.BranchId.HasValue
                    ? await _context.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == user.BranchId)
                    : null;

                result.Add(new UserListDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Roles = roles.ToArray(),
                    IsActive = user.IsActive,
                    Branch = branch == null
                        ? null
                        : new BranchInfoDto
                        {
                            Id = branch.Id,
                            Name = branch.Name,
                            Address = branch.Address
                        }
                });
            }

            return result;
        }

        public async Task<UserDetailDto> GetUserDetailAsync(string userId, string currentUserId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found");

            var roles = await _userManager.GetRolesAsync(user);
            var currentUser = await _userManager.FindByIdAsync(currentUserId);
            if (currentUser == null)
                throw new Exception("User not found");

            var currentUserRoles = await _userManager.GetRolesAsync(currentUser);
            if (currentUserRoles.Contains("Manager") && !currentUserRoles.Contains("Admin"))
            {
                if (!currentUser.BranchId.HasValue || user.BranchId != currentUser.BranchId)
                    throw new Exception("You can only view users in your own branch");
            }

            // Nếu là Customer và không phải chính họ, không được xem
            if (roles.Contains("Customer") && userId != currentUserId && !currentUserRoles.Contains("Admin"))
            {
                throw new Exception("You don't have permission to view this user's details");
            }

            var detail = new UserDetailDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Roles = roles.ToArray(),
                IsActive = user.IsActive
            };

            // Nếu là Manager hoặc Staff, thêm branch info
            if ((roles.Contains("Manager") || roles.Contains("Staff")) && user.BranchId.HasValue)
            {
                var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == user.BranchId);
                if (branch != null)
                {
                    detail.Branch = new BranchInfoDto
                    {
                        Id = branch.Id,
                        Name = branch.Name,
                        Address = branch.Address
                    };
                }
            }

            return detail;
        }

        public async Task<string> CreateCustomerAsync(CreateCustomerDto dto, string? requesterId = null)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                throw new Exception("Email already exists");

            if (!string.IsNullOrWhiteSpace(requesterId))
            {
                var requester = await _userManager.FindByIdAsync(requesterId);
                if (requester == null)
                    throw new Exception("User not found");

                var requesterRoles = await _userManager.GetRolesAsync(requester);
                if (requesterRoles.Contains("Manager") && !requesterRoles.Contains("Admin"))
                {
                    if (!requester.BranchId.HasValue)
                        throw new Exception("Manager has no assigned branch");

                    dto.BranchId = requester.BranchId;
                }
            }

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                PhoneNumber = dto.PhoneNumber,
                BranchId = dto.BranchId,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(user, "Customer");
            _context.Wallets.Add(new Models.Wallet
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Balance = 0m,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
            return user.Id;
        }

        public async Task<string> CreateManagerAsync(CreateManagerDto dto)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                throw new Exception("Email already exists");

            var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == dto.BranchId);
            if (branch == null)
                throw new Exception("Branch not found");

            // Kiểm tra xem branch đã có manager chưa (1-1 relationship)
            if (!string.IsNullOrEmpty(branch.ManagerId))
                throw new Exception("This branch already has a manager");

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                PhoneNumber = dto.PhoneNumber,
                BranchId = dto.BranchId,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(user, "Manager");

            // Gán manager cho branch
            branch.ManagerId = user.Id;
            await _context.SaveChangesAsync();

            return user.Id;
        }

        public async Task<string> CreateStaffAsync(CreateStaffDto dto)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                throw new Exception("Email already exists");

            var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == dto.BranchId);
            if (branch == null)
                throw new Exception("Branch not found");

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                PhoneNumber = dto.PhoneNumber,
                BranchId = dto.BranchId,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(user, "Staff");
            return user.Id;
        }

        public async Task DeleteUserAsync(string userId, string? requesterId = null)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found");

            if (!string.IsNullOrWhiteSpace(requesterId))
            {
                var requester = await _userManager.FindByIdAsync(requesterId);
                if (requester == null)
                    throw new Exception("User not found");

                var requesterRoles = await _userManager.GetRolesAsync(requester);
                var isAdmin = requesterRoles.Contains("Admin");
                var isManager = requesterRoles.Contains("Manager");
                if (!isAdmin && !isManager)
                    throw new Exception("You don't have permission to delete users");

                if (isManager && !isAdmin)
                {
                    if (!requester.BranchId.HasValue || user.BranchId != requester.BranchId)
                        throw new Exception("You can only delete users in your own branch");

                    var targetRoles = await _userManager.GetRolesAsync(user);
                    if (targetRoles.Contains("Admin") || targetRoles.Contains("Manager"))
                        throw new Exception("Manager cannot delete admin or manager accounts");
                }
            }

            // Nếu user là Manager của branch, xóa ManagerId (branch không bị xóa)
            var managerBranches = await _context.Branches
                .Where(b => b.ManagerId == userId)
                .ToListAsync();
            
            foreach (var branch in managerBranches)
            {
                branch.ManagerId = null;
            }

            // Xóa tất cả Vehicles của user (CheckInOut sẽ bị xóa cascade nếu có FK)
            var vehicles = await _context.Vehicle
                .Where(v => v.UserId == userId)
                .ToListAsync();
            
            if (vehicles.Any())
            {
                _context.Vehicle.RemoveRange(vehicles);
            }

            // Xóa tất cả RefreshTokens của user
            var refreshTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId)
                .ToListAsync();
            
            if (refreshTokens.Any())
            {
                _context.RefreshTokens.RemoveRange(refreshTokens);
            }

            // Lưu tất cả thay đổi trước khi xóa user
            await _context.SaveChangesAsync();

            // Xóa user
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        public async Task<List<UserListDto>> GetStaffAsync(string requesterId)
        {
            var requester = await _userManager.FindByIdAsync(requesterId);
            if (requester == null)
                throw new Exception("User not found");

            var requesterRoles = await _userManager.GetRolesAsync(requester);
            var isAdmin = requesterRoles.Contains("Admin");
            var isManager = requesterRoles.Contains("Manager");
            if (!isAdmin && !isManager)
                throw new Exception("You don't have permission to view staff");

            if (isManager && !requester.BranchId.HasValue)
                throw new Exception("Manager has no assigned branch");

            var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
            var staff = staffUsers
                .Where(u => isAdmin || u.BranchId == requester.BranchId)
                .OrderBy(u => u.FullName)
                .ToList();

            var result = new List<UserListDto>();
            foreach (var s in staff)
            {
                var roles = await _userManager.GetRolesAsync(s);
                var branch = s.BranchId.HasValue
                    ? await _context.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == s.BranchId)
                    : null;

                result.Add(new UserListDto
                {
                    Id = s.Id,
                    FullName = s.FullName,
                    Email = s.Email,
                    PhoneNumber = s.PhoneNumber,
                    Roles = roles.ToArray(),
                    IsActive = s.IsActive,
                    Branch = branch == null
                        ? null
                        : new BranchInfoDto
                        {
                            Id = branch.Id,
                            Name = branch.Name,
                            Address = branch.Address
                        }
                });
            }

            return result;
        }

        public async Task<string> CreateStaffByManagerAsync(string managerId, CreateStaffDto dto)
        {
            var manager = await _userManager.FindByIdAsync(managerId);
            if (manager == null)
                throw new Exception("User not found");

            var managerRoles = await _userManager.GetRolesAsync(manager);
            var isAdmin = managerRoles.Contains("Admin");
            var isManager = managerRoles.Contains("Manager");
            if (!isAdmin && !isManager)
                throw new Exception("You don't have permission to create staff");

            if (isManager && !manager.BranchId.HasValue)
                throw new Exception("Manager has no assigned branch");

            // Manager chỉ được tạo staff trong branch của mình; Admin được chọn branch bất kỳ.
            if (isManager && dto.BranchId != manager.BranchId)
                throw new Exception("You can only create staff for your own branch");

            return await CreateStaffAsync(dto);
        }

        public async Task DeleteStaffAsync(string managerId, string staffId)
        {
            var manager = await _userManager.FindByIdAsync(managerId);
            if (manager == null)
                throw new Exception("User not found");

            var managerRoles = await _userManager.GetRolesAsync(manager);
            var isAdmin = managerRoles.Contains("Admin");
            var isManager = managerRoles.Contains("Manager");
            if (!isAdmin && !isManager)
                throw new Exception("You don't have permission to delete staff");

            if (isManager && !manager.BranchId.HasValue)
                throw new Exception("Manager has no assigned branch");

            var staff = await _userManager.FindByIdAsync(staffId);
            if (staff == null)
                throw new Exception("Staff not found");

            var staffRoles = await _userManager.GetRolesAsync(staff);
            if (!staffRoles.Contains("Staff"))
                throw new Exception("User is not staff");

            // Manager chỉ được xóa staff trong branch của mình; Admin được xóa staff bất kỳ.
            if (isManager && staff.BranchId != manager.BranchId)
                throw new Exception("You can only delete staff from your own branch");

            await DeleteUserAsync(staffId);
        }
    }
}
