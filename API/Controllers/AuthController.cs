using Application.DTOs;
using Application.Interfaces;
using Application.Responses;
using Core.Entities;
using Core.Interfaces;
using Core.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Data;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        readonly IAuthService _authService;
        readonly IServices<RefreshToken> _refreshToken;
        readonly UserManager<User> _userManager;
        readonly RoleManager<Role> _roleManager;
        readonly ILogger<AuthController> _logger;
        readonly JwtSettings _jwtSettings;

        public AuthController(IAuthService authService, UserManager<User> userManager, RoleManager<Role> roleManager, ILogger<AuthController> logger, IOptions<JwtSettings> jwtSettings, IServices<RefreshToken> refreshToken)
        {
            _authService = authService;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _jwtSettings = jwtSettings.Value;
            _refreshToken = refreshToken;
        }


        private void GenerateCookies(AuthServiceResponse response)
        {
            _logger.LogInformation("Generating cookies in 'GenerateCookies' method");

            try
            {
                var accessTokenCookie = new CookieOptions()
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.Now.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
                };

                var refreshTokenCookie = new CookieOptions()
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.Now.AddDays(_jwtSettings.RefreshTokenExpirationDays)
                };

                Response.Cookies.Append("accessToken", response.AccessToken, accessTokenCookie);
                Response.Cookies.Append("refreshToken", response.RefreshToken, refreshTokenCookie);

                _logger.LogInformation("Cookies generated successfully\n");
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Some thing went wrong while creating the cookies, {errer}", ex);
            }
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            try
            {
                AuthServiceResponse result = await _authService.Login(request);

                if (!result.IsSuccess) return BadRequest(result);

                GenerateCookies(result);

                User? user = await _userManager.FindByEmailAsync(request.Email);
                var role = await _userManager.GetRolesAsync(user);

                _logger.LogInformation("User Login successful ");

                return Ok(new ApiResponse<UserInfoDto>
                {
                    Success = true,
                    Message = "Login successful",
                    Data = new UserInfoDto
                    {
                        UserId = user.Id,
                        UserName = user.Fname[0..1].ToUpper() + user.Lname[0..1].ToUpper(),
                        Role = role.ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Some thing went wrong while login, {errer}", ex);

                return StatusCode(500, new { message = "An unexpected error occurred. Please try again later." });
            }
        }

        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto request)
        {
            try
            {
                AuthServiceResponse result = await _authService.Register(request);

                if (!result.IsSuccess) return BadRequest(result);

                GenerateCookies(result);

                User? user = await _userManager.FindByEmailAsync(request.Email);
                var role = await _userManager.GetRolesAsync(user);

                return Ok(new ApiResponse<UserInfoDto>
                {
                    Success = true,
                    Message = "Login successful",
                    Data = new UserInfoDto
                    {
                        UserId = user.Id,
                        UserName = user.Fname[0..1].ToUpper() + user.Lname[0..1].ToUpper(),
                        Role = role.ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Some thing went wrong while login, {errer}", ex);

                return StatusCode(500, new { message = "An unexpected error occurred. Please try again later." });
            }
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

            if (!result.IsSuccess)
            {
                _logger.LogInformation("Invalid refresh token!\n");
                return Unauthorized();
            }

            GenerateCookies(result);

            _logger.LogInformation("Tokens refreshed successfully\n");

            RefreshToken? token = await _refreshToken.GetAsync(rt => rt.Token == refreshToken, true, rt => rt.User);

            var roles = await _userManager.GetRolesAsync(token?.User);

            return Ok(new ApiResponse<UserInfoDto>
            {
                Success = true,
                Message = "Tokens refreshed",
                Data = new UserInfoDto
                {
                    UserId = token?.User.Id,
                    UserName = token?.User.Fname[0..1].ToUpper() + token?.User.Lname[0..1].ToUpper(),
                    Role = roles.ToList(),
                }
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                string? refreshToken = Request.Cookies["refreshToken"];

                var result = await _authService.Logout(refreshToken);

                Response.Cookies.Delete("accessToken", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax
                });

                Response.Cookies.Delete("refreshToken", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax
                });

                _logger.LogInformation("User logged out successfully");

                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { message = "Logout failed" });
            }
        }

        [HttpPost("assign-role")]
        public async Task<IActionResult> AssignRole([FromBody] UpdatePermissionDto request)
        {
            try
            {
                var result = await _authService.AssignRoleAsync(request);

                if (!result.IsSuccess)
                {
                    _logger.LogInformation("Fail to assigned the Role");

                    return Ok(new ApiResponse<string>()
                    {
                        Success = false,
                        Message = result.Message
                    });
                }

                _logger.LogInformation("Role assigned successfully");

                return Ok(new ApiResponse<string>()
                {
                    Success = true,
                    Message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during role assignment");
                return StatusCode(500, new { message = "Role assignment failed" });
            }
        }
    }
}