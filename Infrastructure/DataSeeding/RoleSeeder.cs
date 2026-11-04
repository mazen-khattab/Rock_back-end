using Core.Entities;
using Core.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Infrastructure.DataSeeding
{
    public static class RoleSeeder
    {
        public static async Task SeedAsync(RoleManager<Role> roleManager, ILogger logger)
        {
            logger.LogInformation("Seeding application roles");

            var roles = new List<RoleType> { RoleType.User, RoleType.Admin, RoleType.Owner };

            // Create only missing roles so startup can safely run again.
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName.ToString()))
                {
                    logger.LogInformation("Creating role: {RoleName}", roleName);
                    await roleManager.CreateAsync(new Role
                    {
                        Name = roleName.ToString(),
                        NormalizedName = roleName.ToString().ToUpper(),
                        ConcurrencyStamp = Guid.NewGuid().ToString()
                    });
                }
            }
        }
    }
}
