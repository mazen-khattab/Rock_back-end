using System.Security.Claims;
using Core.ExceptionsTypes;

namespace API.Extensions
{
    public static class UserClaimsExtensions
    {
        public static int GetRequiredUserId(this ClaimsPrincipal principal)
        {
            if (!int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out int id) || id <= 0)
                throw new UnauthorizedException("Invalid account identity.");
            return id;
        }

        public static int? GetOptionalUserId(this ClaimsPrincipal principal) =>
            principal.Identity?.IsAuthenticated == true ? principal.GetRequiredUserId() : null;
    }
}
