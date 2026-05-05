using Microsoft.AspNetCore.Identity;
using SmartParking.Models.Identity;

namespace SmartParking.Data.Seed
{
    public static class UserSeeder
    {
        public static async Task SeedAdminUser(UserManager<ApplicationUser> userManager)
        {
            var adminEmail = "admin@gmail.com";
            var user = await userManager.FindByEmailAsync(adminEmail);

            if (user == null)
            {
                user = new ApplicationUser
                {

                    UserName = adminEmail,
                    FullName = "System Admin",
                    Email = adminEmail,
                    EmailConfirmed = true,
                    IsActive = true
                };


                var result = await userManager.CreateAsync(user, "Admin123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Admin");
                    System.Diagnostics.Debug.WriteLine("=== SEED ADMIN USER SUCCESS ===");
                }
                else
                {
                    // In lỗi ra cửa sổ Output để bạn biết chính xác tại sao fail
                    foreach (var error in result.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"=== SEED ERROR: {error.Description} ===");
                    }
                }
            }
        }
    }
}