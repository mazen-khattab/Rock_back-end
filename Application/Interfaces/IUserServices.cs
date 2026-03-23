using Application.DTOs;
using Application.Responses;
using Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IUserServices
    {
        Task<ApiResponse<User>> GetUserProfile(string userId);
        Task<ApiResponse<User>> UpdateUserProfile(UserProfileDto userProfileDto, string userId);
        Task<ApiResponse<string>> ChangePassword(string oldPassword, string newPassword, string confirmedPassword, string userId);
    }
}
