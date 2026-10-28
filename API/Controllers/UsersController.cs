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


        private string GetUserId()
        {
            _logger.LogInformation("Retrieving user ID from claims in 'GetUserId' method");

            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("Unauthorize user");
        }

        [HttpGet]
        [Authorize]
        [Route("profile")]
        public async Task<IActionResult> GetProfile()
        {
            string userId = GetUserId();

            _logger.LogInformation("User {UserId} is attempting to retrieve their profile.", userId);
            var result = await _userServices.GetUserProfile(userId);

            if (result is not null && result.IsSuccess)
            {
                var userProfileDto = result.Data;
                _logger.LogInformation("Profile successfully retrieved for User {UserId}.", userId);

                return Ok(new ApiResponse<UserProfileDto>
                {
                    IsSuccess = true,
                    Message = "Profile retrieved successfully",
                    Data = userProfileDto
                });
            }


            _logger.LogWarning("Profile retrieval failed for User {UserId}. Reason: {Message}", userId, result?.Message ?? "");
            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Profile retrieval failed.",
                Path = "api/Users/profile",
                Method = HttpMethods.Get,
                TraceId = HttpContext.TraceIdentifier
            };

            _logger.LogWarning("Returning error response for User: {UserId} with StatusCode: {StatusCode}, Message: {Message}",
                userId, errorResponse.StatusCode, errorResponse.Message);

            return BadRequest(errorResponse);
        }

        [HttpPut]
        [Authorize]
        [Route("profile/update")]
        public async Task<IActionResult> UpdateProfile([FromBody] UserProfileDto dto)
        {
            string userId = GetUserId();

            _logger.LogInformation("User {UserId} is attempting to update their profile data.", userId);
            var result = await _userServices.UpdateUserProfile(dto, userId);

            if (result is not null && result.IsSuccess)
            {
                var newUserProfileDto = result.Data;

                _logger.LogInformation("Profile successfully updated for User {UserId}.", userId);
                return Ok(new ApiResponse<UserProfileDto>
                {
                    IsSuccess = true,
                    Message = "Profile updated successfully",
                    Data = newUserProfileDto
                });
            }

            _logger.LogWarning("Profile update failed for User {UserId}. Reason: {Message}", userId, result?.Message ?? "");
            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Profile update failed.",
                Path = "api/Users/profile/update",
                Method = HttpMethods.Put,
                TraceId = HttpContext.TraceIdentifier
            };

            _logger.LogWarning("Returning error response for User: {UserId} with StatusCode: {StatusCode}, Message: {Message}",
                userId, errorResponse.StatusCode, errorResponse.Message);

            return BadRequest(errorResponse);
        }

        [HttpPut]
        [Authorize]
        [Route("password/change")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            string userId = GetUserId();

            _logger.LogInformation("User {UserId} is attempting to change their password.", userId);
            var result = await _userServices.ChangePassword(request.OldPassword, request.NewPassword, request.ConfirmedPassword, userId);

            if (result is not null && result.IsSuccess)
            {
                _logger.LogInformation("Password successfully changed for User {UserId}.", userId);
                return Ok(result);
            }

            _logger.LogWarning("Password change failed for User {UserId}. Reason: {Message}", userId, result?.Message ?? "");
            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Password change failed.",
                Path = "api/Users/password/change",
                Method = HttpMethods.Put,
                TraceId = HttpContext.TraceIdentifier
            };

            _logger.LogWarning("Returning error response for User: {UserId} with StatusCode: {StatusCode}, Message: {Message}",
                userId, errorResponse.StatusCode, errorResponse.Message);

            return BadRequest(errorResponse);
        }
    }
}
