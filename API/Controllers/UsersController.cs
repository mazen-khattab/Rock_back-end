using System.ComponentModel.DataAnnotations;
using API.Extensions;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserServices _userServices;
        private readonly ILogger<UsersController> _logger;
        private readonly IAccountManagementService _accountManagement;

        public UsersController(IUserServices userServices, ILogger<UsersController> logger, IAccountManagementService accountManagement)
        {
            _userServices = userServices;
            _logger = logger;
            _accountManagement = accountManagement;
        }

        /// <summary>
        /// Lists Admin and Owner accounts for the management panel.
        /// </summary>
        [HttpGet("admin")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> ListUsers([FromQuery] AccountFilterDto filter)
        {
            _logger.LogInformation("Staff list requested by actor ID: {ActorId}", User.GetRequiredUserId());
            var result = await _accountManagement.ListStaff(filter);
            return result.IsSuccess ? Ok(result.Data) : this.ToApiResult(result);
        }

        [HttpGet("admin/{id:int}")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> GetUserDetails([Range(1, int.MaxValue)] int id)
        {
            _logger.LogInformation("Staff details requested for ID: {AccountId}", id);
            return this.ToApiResult(await _accountManagement.GetStaff(id));
        }

        /// <summary>
        /// Creates staff through Identity without changing the caller's login cookies.
        /// </summary>
        [HttpPost("admin/create")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> CreateUser([FromBody] CreateStaffDto dto)
        {
            var result = await _accountManagement.CreateStaff(dto, User.GetRequiredUserId());
            return result.IsSuccess
                ? CreatedAtAction(nameof(GetUserDetails), new { id = result.Data!.Id }, result)
                : this.ToApiResult(result);
        }

        [HttpPut("admin/{id:int}/edit")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> UpdateUser([Range(1, int.MaxValue)] int id, [FromBody] UpdateStaffDto dto) =>
            this.ToApiResult(await _accountManagement.UpdateStaff(id, dto, User.GetRequiredUserId()));

        /// <summary>
        /// Deletes orderless staff accounts; all historical orders prevent deletion.
        /// </summary>
        [HttpDelete("admin/{id:int}/delete")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> DeleteUser([Range(1, int.MaxValue)] int id) =>
            this.ToApiResult(await _accountManagement.DeleteStaff(id, User.GetRequiredUserId()));

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            int userId = User.GetRequiredUserId();
            _logger.LogInformation("Profile requested for user ID: {UserId}", userId);
            return this.ToApiResult(await _userServices.GetUserProfile(userId.ToString()));
        }

        [HttpPut("profile/update")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UserProfileDto dto)
        {
            int userId = User.GetRequiredUserId();
            _logger.LogInformation("Profile update requested for user ID: {UserId}", userId);
            return this.ToApiResult(await _userServices.UpdateUserProfile(dto, userId.ToString()));
        }

        [HttpPut("password/change")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            int userId = User.GetRequiredUserId();
            _logger.LogInformation("Password change requested for user ID: {UserId}", userId);
            return this.ToApiResult(await _userServices.ChangePassword(
                request.OldPassword, request.NewPassword, request.ConfirmedPassword, userId.ToString()));
        }
    }
}
