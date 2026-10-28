using Application.DTOs;
using Application.Interfaces;
using Application.Mapping;
using Application.Responses;
using Core.Entities;
using Core.Enum;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;
using static System.Net.WebRequestMethods;

namespace Application.Services
{
    public class UserServices : IUserServices
    {
        private readonly UserManager<User> _userManager;
        private readonly ILogger<UserServices> _logger;

        public UserServices(UserManager<User> userManager, ILogger<UserServices> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<ApiResponse<UserProfileDto>> GetUserProfile(string userId)
        {
            _logger.LogInformation("Fetching profile for user ID: {UserId}", userId);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                _logger.LogInformation("Cannot find user with Id: {userID}", userId);

                return new()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.NotFound,
                    Message = "User not found"
                };
            }

            _logger.LogInformation("Profile retrieved successfully for userId: {userId}", userId);

            var userProfileDto = user.ToDto() ?? new();
            return new()
            {
                IsSuccess = true,
                Message = "Profile retrieved successfully",
                Data = userProfileDto
            };
        }

        public async Task<ApiResponse<UserProfileDto>> UpdateUserProfile(UserProfileDto dto, string userId)
        {
            _logger.LogInformation("Updating profile for User: {userId}", userId);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                _logger.LogInformation("Cannot find user with Id: {userID}", userId);

                return new()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.NotFound,
                    Message = "User not found"
                };
            }

            user.FromDto(dto);

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                _logger.LogError("Failed to update user {UserId}: {Errors}", user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
                return new() 
                { 
                    IsSuccess = false, 
                    ErrorType = ErrorType.ValidationError,
                    Message = "Update failed" 
                };
            }

            _logger.LogInformation("Profile updated successfully for userId: {userId}", userId);

            var userProfileDto = user.ToDto() ?? new();
            return new() 
            { 
                IsSuccess = true, 
                Message = "Profile updated successfully", 
                Data = userProfileDto
            };
        }

        public async Task<ApiResponse<string>> ChangePassword(string oldPassword, string newPassword, string confirmedPassword, string userId)
        {
            _logger.LogInformation("Changing password for user: {usreId}", userId);

            if (newPassword != confirmedPassword)
            {
                _logger.LogInformation("Password change failed for User { UserId}: New password and confirmation do not match.", userId);

                return new()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.ValidationError,
                    Message = "New password and confirmation do not match"
                };
            }

            if (oldPassword == newPassword)
            {
                _logger.LogWarning("Password change failed for User {UserId}: New password is identical to the old password.", userId);

                return new()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.ValidationError,
                    Message = "New password is identical to the old password."
                };
            }
                
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                _logger.LogInformation("Cannot find user with Id: {userID}", userId);

                return new()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.NotFound,
                    Message = "User not found"
                };
            }

            var result = await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);

            if (!result.Succeeded)
            {
                _logger.LogInformation("Something went wrong while changing user password");
                string errorMessage = "";

                foreach (var error in result.Errors)
                {
                    _logger.LogInformation("changing password error: {error}", error.Description);

                    errorMessage += error.Description;
                }

                return new()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.ValidationError,
                    Message = errorMessage
                };
            }

            _logger.LogInformation("Password changed successfully for userId: {userId}", userId);
            return new() 
            { 
                IsSuccess = true, 
                Message = "Password changed successfully" 
            };
        }
    }
}
