using Application.DTOs;
using Application.Interfaces;
using Application.Responses;
using Core.Entities;
using Core.Interfaces;
using Core.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        readonly IAuthService _authService;
        readonly IServices<RefreshToken> _refreshToken;
        readonly UserManager<User> _userManager;
        readonly ILogger<AuthController> _logger;
        readonly JwtSettings _jwtSettings;

        public AuthController(IAuthService authService, UserManager<User> userManager, ILogger<AuthController> logger, IOptions<JwtSettings> jwtSettings, IServices<RefreshToken> refreshToken)
        {
            _authService = authService;
            _userManager = userManager;
            _logger = logger;
            _jwtSettings = jwtSettings.Value;
            _refreshToken = refreshToken;
        }


        private void GenerateCookies(AuthServiceResponse response)
        {
            _logger.LogInformation("Generating cookies for Access and Refresh tokens...");

            try
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    IsEssential = true
                };

                var accessTokenCookie = cookieOptions;
                accessTokenCookie.Expires = DateTime.Now.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

                var refreshTokenCookie = cookieOptions;
                refreshTokenCookie.Expires = DateTime.Now.AddDays(_jwtSettings.RefreshTokenExpirationDays);

                Response.Cookies.Append("accessToken", response.AccessToken, accessTokenCookie);
                Response.Cookies.Append("refreshToken", response.RefreshToken, refreshTokenCookie);

                _logger.LogInformation("Cookies generated and appended successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Something went wrong while generating the cookies.");
            }
        }

        private ApiResponse<UserInfoDto> GetUserInfo(string message, User user, List<string> role)
        {
            return new ApiResponse<UserInfoDto>
            {
                IsSuccess = true,
                Message = "Login successful",
                Data = new UserInfoDto
                {
                    UserId = user.Id,
                    UserName = user.Fname[0..1].ToUpper() + user.Lname[0..1].ToUpper(),
                    Role = role.ToList()
                }
            };
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            AuthServiceResponse result = await _authService.Login(request);

            if (result is not null && result.IsSuccess)
            {
                GenerateCookies(result);

                User? user = await _userManager.FindByEmailAsync(request.Email);
                var role = await _userManager.GetRolesAsync(user);

                _logger.LogInformation("User Login successful ");

                return Ok(GetUserInfo("Login successful", user, role.ToList()));
            }

            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Failed to login.",
                Path = "api/Auth/Login",
                Method = HttpMethods.Post,
                TraceId = HttpContext.TraceIdentifier
            };

            _logger.LogWarning("Returning error response with StatusCode: {StatusCode}, Message: {Message}", errorResponse.StatusCode, errorResponse.Message);

            return BadRequest(errorResponse);
        }

        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto request)
        {
            AuthServiceResponse result = await _authService.Register(request);

            if (result is not null && result.IsSuccess)
            {
                GenerateCookies(result);

                User? user = await _userManager.FindByEmailAsync(request.Email);
                var role = await _userManager.GetRolesAsync(user);

                return Ok(GetUserInfo("Register successful", user, role.ToList()));
            }

            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Failed to Register.",
                Path = "api/Auth/Register",
                Method = HttpMethods.Post,
                TraceId = HttpContext.TraceIdentifier
            };

            _logger.LogWarning("Returning error response with StatusCode: {StatusCode}, Message: {Message}", errorResponse.StatusCode, errorResponse.Message);

            return BadRequest(errorResponse);
        }

        [HttpPost]
        [Route("Refresh")]
        public async Task<IActionResult> Refresh()
        {
            string? refreshToken = Request.Cookies["refreshToken"];

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogInformation("Refresh token does not exist in cookies");
                return Unauthorized("Refresh token does not exist in cookies");
            }

            var result = await _authService.ValidateRefreshToken(refreshToken);

            if (result is not null && result.IsSuccess)
            {
                GenerateCookies(result);

                _logger.LogInformation("Tokens refreshed successfully\n");

                RefreshToken? token = await _refreshToken.GetAsync(rt => rt.Token == refreshToken, true, rt => rt.User);

                var roles = await _userManager.GetRolesAsync(token?.User);

                return Ok(GetUserInfo("Tokens refreshed", token.User, roles.ToList()));

            }

            _logger.LogInformation("Invalid refresh token!\n");
            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status401Unauthorized,
                Message = result?.Message ?? "Failed to Refresh.",
                Path = "api/Auth/Refresh",
                Method = HttpMethods.Post,
                TraceId = HttpContext.TraceIdentifier
            };

            _logger.LogWarning("Returning error response with StatusCode: {StatusCode}, Message: {Message}", errorResponse.StatusCode, errorResponse.Message);

            return Unauthorized(errorResponse);
        }

        [HttpGet]
        [Route("UserInfo")]
        [Authorize]
        public async Task<IActionResult> GetUserInfo()
        {
            _logger.LogInformation("[AuthController] Attempting to retrieve user info from token claims...");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("[AuthController] Failed to retrieve UserInfo: User ID claim is missing or empty.");
                var errorResponse = new ApiErrorResponse
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status401Unauthorized,
                    Message = "Failed to retrieve UserInfo",
                    Path = "api/Auth/UserInfo",
                    Method = HttpMethods.Get,
                    TraceId = HttpContext.TraceIdentifier
                };

                _logger.LogWarning("Returning error response with StatusCode: {StatusCode}, Message: {Message}", errorResponse.StatusCode, errorResponse.Message);

                return Unauthorized(errorResponse);
            }

            var result = await _authService.GetUserInfoAsync(userId);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("[AuthController] Failed to process UserInfo: Service returned null for UserId: {UserId}", userId);
                var errorResponse = new ApiErrorResponse
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status401Unauthorized,
                    Message = result?.Message ?? "Failed to process UserInfo",
                    Path = "api/Auth/UserInfo",
                    Method = HttpMethods.Get,
                    TraceId = HttpContext.TraceIdentifier
                };

                _logger.LogWarning("Returning error response with StatusCode: {StatusCode}, Message: {Message}", errorResponse.StatusCode, errorResponse.Message);

                return Unauthorized(errorResponse);
            }

            _logger.LogInformation("[AuthController] Successfully processed UserInfo request for UserId: {UserId}", userId);

            return Ok(new ApiResponse<UserInfoDto>
            {
                IsSuccess = true,
                Message = "User retrieved successfully",
                Data = result.Data
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation("[AuthController] Attempting to logout user...");

            string? refreshToken = Request.Cookies["refreshToken"];

            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                var result = await _authService.Logout(refreshToken);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("[AuthController] Logout Service Warning: {Message}", result.Message);
                }
            }
            else
            {
                _logger.LogInformation("[AuthController] No refresh token in cookies. Proceeding to clear local cookies.");
            }

            var deleteCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                IsEssential = true
            };

            Response.Cookies.Delete("accessToken", deleteCookieOptions);
            Response.Cookies.Delete("refreshToken", deleteCookieOptions);

            _logger.LogInformation("[AuthController] User logged out successfully and cookies cleared.");

            return Ok(new ApiResponse<string>
            {
                IsSuccess = true,
                Message = "Logged out successfully"
            });
        }

        [HttpPost]
        [Route("assign-role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignRole([FromBody] UpdatePermissionDto request)
        {
            _logger.LogInformation("Attempting to assign role. Role: {Role}, TargetUser: {TargetUser}", request.Role, request.UserId);

            var result = await _authService.AssignRoleAsync(request);

            if (result is not null && result.IsSuccess)
            {
                _logger.LogInformation("Role '{Role}' assigned successfully to user '{TargetUser}'", request.Role, request.UserId);

                return Ok(new ApiResponse<string>
                {
                    IsSuccess = true,
                    Message = result.Message,
                    Data = request.Role
                });
            }

            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Failed to assign role.",
                Path = "api/Auth/assign-role",
                Method = HttpMethods.Post,
                TraceId = HttpContext.TraceIdentifier
            };

            _logger.LogWarning("Failed to assign role '{Role}' to user '{TargetUser}'. Returning error response with StatusCode: {StatusCode}, Message: {Message}",
                request.Role, request.UserId, errorResponse.StatusCode, errorResponse.Message);

            return BadRequest(errorResponse);
        }
    }
}