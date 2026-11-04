using Core.Entities;
using Core.Enums;
using Core.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Infrastructure.DataSeeding
{
    public static class AdminUserSeeder
    {
        public static async Task SeedAsync(UserManager<User> userManager, AdmincredentialsSettings adminSettings, ILogger logger)
        {
            logger.LogInformation("Seeding initial administrator");

            // Preserve the existing rule: create the administrator only when there are no users.
            if (userManager.Users.Any())
            {
                logger.LogDebug("Users already exist; skipping administrator seeding");
                return;
            }

            var user = new User
            {
                Fname = adminSettings.Fname,
                Lname = adminSettings.Lname,
                UserName = adminSettings.UserName,
                Email = adminSettings.Email,
                PhoneNumber = adminSettings.PhoneNumber
            };

            await userManager.CreateAsync(user, adminSettings.Password);

            // Roles must be seeded before assigning them to the initial administrator.
            var roles = new List<RoleType> { RoleType.User, RoleType.Admin, RoleType.Owner };
            foreach (var roleName in roles)
            {
                await userManager.AddToRoleAsync(user, roleName.ToString());
            }
        }
    }
}
