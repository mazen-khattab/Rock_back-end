using Core.Entities;
using Core.Enums;
using Core.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Infrastructure.DataSeeding
{
    public static class Seeding
    {
        public static async Task SeedingAsync(RoleManager<Role> roleManager, UserManager<User> userManager, IOptions<AdmincredentialsSettings> adminSettings)
        {
            await SeedingAsync(roleManager, userManager, adminSettings?.Value ?? new AdmincredentialsSettings());
        }

        public static async Task SeedingAsync(RoleManager<Role> roleManager, UserManager<User> userManager, AdmincredentialsSettings adminSettings)
        {
            // Seed Roles
            var roles = new List<RoleType> { RoleType.User, RoleType.Admin, RoleType.Owner };

            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName.ToString()))
                {
                    await roleManager.CreateAsync(new Role
                    {
                        Name = roleName.ToString(),
                        NormalizedName = roleName.ToString().ToUpper(),
                        ConcurrencyStamp = Guid.NewGuid().ToString()
                    });
                }
            }

            // Seed Admin User
            if (!userManager.Users.Any())
            {
                var user = new User()
                {
                    Fname = adminSettings.Fname,
                    Lname = adminSettings.Lname,
                    UserName = adminSettings.UserName,
                    Email = adminSettings.Email,
                    PhoneNumber = adminSettings.PhoneNumber
                };

                await userManager.CreateAsync(user, adminSettings.Password);

                foreach (var roleName in roles)
                {
                    await userManager.AddToRoleAsync(user, roleName.ToString());
                }
            }
        }
    }
}
