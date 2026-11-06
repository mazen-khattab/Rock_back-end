using System.Security.Claims;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

namespace API.Extensions
{
    public static class AccountTokenValidation
    {
        public static async Task ValidateAsync(TokenValidatedContext context)
        {
            if (!int.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out int id))
            {
                context.Fail("Invalid account identity.");
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();

            var account = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == id && !u.IsDeleted)
                .Select(u => new
                {
                    Roles = u.UserRoles
                        .Where(ur => !ur.IsDeleted && !ur.Role.IsDeleted)
                        .Select(ur => ur.Role.Name!).ToList()
                })
                .SingleOrDefaultAsync(context.HttpContext.RequestAborted);

            // JWT claims must not retain privileges after an account deletion or role change.
            var claimedRoles = context.Principal!
                .FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (account == null || !claimedRoles.SetEquals(account.Roles))
            {
                context.Fail("The account is no longer active or its permissions changed. Sign in again.");
            }
        }
    }
}
