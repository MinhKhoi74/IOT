using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using SmartParking.Models.Identity;

namespace SmartParking.SignalR
{
    [Authorize]
    public class ParkingHub : Hub
    {
        public const string AdminGroup = "parking_admins";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ParkingHub> _logger;

        public ParkingHub(UserManager<ApplicationUser> userManager, ILogger<ParkingHub> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public static string BranchGroup(Guid branchId) => $"parking_branch_{branchId:N}";

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    if (roles.Contains("Admin"))
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup);
                    }

                    if (user.BranchId.HasValue)
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, BranchGroup(user.BranchId.Value));
                    }

                    _logger.LogInformation("User {UserId} connected to ParkingHub", userId);
                }
            }

            await base.OnConnectedAsync();
        }
    }
}
