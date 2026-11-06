using API.Extensions;
using Application.DTOs;
using Application.Interfaces;
using Application.Responses;
using Core.Entities;
using Core.Enum;
using Core.Interfaces;
using Core.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IServices<RefreshToken> _refreshToken;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<AuthController> _logger;
        private readonly JwtSettings _jwtSettings;

        public AuthController(IAuthService authService, UserManager<User> userManager, ILogger<AuthController> logger,
            IOptions<JwtSettings> jwtSettings, IServices<RefreshToken> refreshToken)
        {
            _authService = authService;
            _userManager = userManager;
            _logger = logger;
            _jwtSettings = jwtSettings.Value;
            _refreshToken = refreshToken;
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            _logger.LogInformation("Login requested");
            var result = await _authService.Login(request);
            if (result?.IsSuccess != true)
                return this.ToApiResult<UserInfoDto>(result);

            return await CompleteAuthentication(result, await _userManager.FindByEmailAsync(request.Email));
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto request)
        {
            _logger.LogInformation("Registration requested");
            var result = await _authService.Register(request);
            if (result?.IsSuccess != true)
                return this.ToApiResult<UserInfoDto>(result);

            return await CompleteAuthentication(result, await _userManager.FindByEmailAsync(request.Email));
        }

        [HttpPost("Refresh")]
        public async Task<IActionResult> Refresh()
        {
            string? refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrWhiteSpace(refreshToken))
                return this.ToApiResult(new ApiResponse<UserInfoDto>
                {
                    ErrorType = ErrorType.InvalidCredentials,
                    Message = "Refresh token does not exist in cookies."
                });

            _logger.LogInformation("Token refresh requested");
            var result = await _authService.ValidateRefreshToken(refreshToken);
            if (result?.IsSuccess != true)
                return this.ToApiResult<UserInfoDto>(result, failureStatusCode: StatusCodes.Status401Unauthorized);

            var token = await _refreshToken.GetAsync(rt => rt.Token == refreshToken, true, rt => rt.User);
            return await CompleteAuthentication(result, token?.User);
        }

        [HttpGet("UserInfo")]
        [Authorize]
        public async Task<IActionResult> GetUserInfo()
        {
            int userId = User.GetRequiredUserId();
            _logger.LogInformation("User information requested for ID: {UserId}", userId);
            return this.ToApiResult(await _authService.GetUserInfoAsync(userId.ToString()));
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation("Logout requested");
            string? refreshToken = Request.Cookies["refreshToken"];
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                var result = await _authService.Logout(refreshToken);
                if (result?.IsSuccess == false)
                    _logger.LogWarning("Logout service reported: {Message}", result.Message);
            }

            // Clear local cookies even if the refresh token is already invalid.
            Response.ClearAuthenticationCookies();
            return this.ToApiResult(new ApiResponse<string> { IsSuccess = true, Message = "Logged out successfully" });
        }

        [HttpPost("assign-role")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> AssignRole([FromBody] UpdatePermissionDto request)
        {
            _logger.LogInformation("Role assignment requested for user ID: {UserId}, role: {Role}", request.UserId, request.Role);
            return this.ToApiResult(await _authService.AssignRoleAsync(request), request.Role);
        }

        private async Task<IActionResult> CompleteAuthentication(AuthServiceResponse result, User? user)
        {
            if (user == null)
                return this.ToApiResult(new ApiResponse<UserInfoDto>
                {
                    ErrorType = ErrorType.InvalidCredentials,
                    Message = "The account is no longer available."
                });

            var roles = await _userManager.GetRolesAsync(user);
            var userInfo = new UserInfoDto
            {
                UserId = user.Id,
                UserName = Initial(user.Fname) + Initial(user.Lname),
                Role = roles.ToList()
            };
            Response.SetAuthenticationCookies(result, _jwtSettings);
            _logger.LogInformation("Authentication completed for user ID: {UserId}", user.Id);
            return this.ToApiResult(result, userInfo);
        }

        private static string Initial(string name) => string.IsNullOrEmpty(name) ? string.Empty : name[..1].ToUpperInvariant();
    }
}
