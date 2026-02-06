using Core.Entities;
using Core.Enums;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.DataSeeding
{
    public static class Seeding
    {
        public static async Task SeedingAsync(RoleManager<Role> roleManager, UserManager<User> userManager)
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
                    Fname = "mazen",
                    Lname = "khattab",
                    UserName = "MK",
                    Email = "mazenkhtab11@gmail.com",
                    PhoneNumber = "01023839637"
                };

                await userManager.CreateAsync(user, "Mak.12");

                foreach (var roleName in roles)
                {
                    await userManager.AddToRoleAsync(user, roleName.ToString());
                }
            }
        }
    }
}
