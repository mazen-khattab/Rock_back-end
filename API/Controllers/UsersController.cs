using Application.DTOs;
using Application.Interfaces;
using Application.Mapping;
using Application.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserServices _userServices;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserServices userServices, ILogger<UsersController> logger, IMapper mapper)
        {
            _userServices = userServices;
            _logger = logger;
        }


        [HttpGet]
        [Authorize]
        [Route("profile")]
        public async Task<IActionResult> GetProfile()
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            _logger.LogInformation("User {UserId} is attempting to retrieve their profile.", userId);
            var result = await _userServices.GetUserProfile(userId);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Profile retrieval failed for User {UserId}. Reason: {Message}", userId, result.Message);
                return NotFound(result);
            }

            var userProfileDto = result.Data?.ToDto() ?? new();
            _logger.LogInformation("Profile successfully retrieved for User {UserId}.", userId);

            return Ok(new ApiResponse<UserProfileDto>
            {
                IsSuccess = true,
                Message = "Profile retrieved successfully",
                Data = userProfileDto
            });

        }

        [HttpPut]
        [Authorize]
        [Route("profile/update")]
        public async Task<IActionResult> UpdateProfile([FromBody] UserProfileDto dto)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            _logger.LogInformation("User {UserId} is attempting to update their profile data.", userId);
            var result = await _userServices.UpdateUserProfile(dto, userId);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Profile update failed for User {UserId}. Reason: {Message}", userId, result.Message);
                return BadRequest(result);
            }

            var newUserProfileDto = result.Data?.ToDto() ?? new();

            _logger.LogInformation("Profile successfully updated for User {UserId}.", userId);
            return Ok(new ApiResponse<UserProfileDto>
            {
                IsSuccess = true,
                Message = "Profile updated successfully",
                Data = newUserProfileDto
            });
        }

        [HttpPost]
        [Authorize]
        [Route("password/change")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            _logger.LogInformation("User {UserId} is attempting to change their password.", userId);
            var result = await _userServices.ChangePassword(request.OldPassword, request.NewPassword, request.ConfirmedPassword, userId);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Password change failed for User {UserId}. Reason: {Message}", userId, result.Message);
                return BadRequest(result);
            }

            _logger.LogInformation("Password successfully changed for User {UserId}.", userId);
            return Ok(result);
        }
    }
}
