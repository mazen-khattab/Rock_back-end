using Application.Responses;
using Core.Settings;

namespace API.Extensions
{
    public static class AuthenticationCookieExtensions
    {
        public static void SetAuthenticationCookies(this HttpResponse response, AuthServiceResponse tokens, JwtSettings settings)
        {
            // Use separate options so each token keeps its own expiration.
            response.Cookies.Append("accessToken", tokens.AccessToken, CreateOptions(DateTime.Now.AddMinutes(settings.AccessTokenExpirationMinutes)));
            response.Cookies.Append("refreshToken", tokens.RefreshToken, CreateOptions(DateTime.Now.AddDays(settings.RefreshTokenExpirationDays)));
        }

        public static void ClearAuthenticationCookies(this HttpResponse response)
        {
            var options = CreateOptions();
            options.IsEssential = true;
            response.Cookies.Delete("accessToken", options);
            response.Cookies.Delete("refreshToken", options);
        }

        private static CookieOptions CreateOptions(DateTimeOffset? expires = null) => new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = expires
        };
    }
}
