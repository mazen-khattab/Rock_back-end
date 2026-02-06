using Application.DTOs;
using Application.Responses;
using Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthServiceResponse> Login(LoginDto request);
        Task<AuthServiceResponse> Register(RegisterDto request);
        Task<string> CreateAccessToken(User user);
        Task<string> CreateRefreshToken(User user);
        Task<AuthServiceResponse> AssignRoleAsync(UpdatePermissionDto updatePermissionDto);
        Task<AuthServiceResponse> ValidateRefreshToken(string token);
        Task<AuthServiceResponse> Logout(string refreshToken);
    }
}
