using Application.DTOs;
using Application.Interfaces;
using Application.Responses;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Application.Services
{
    public class AuthService : IAuthService
    {
        readonly IServices<RefreshToken> _refreshToken;
        readonly ILogger<AuthService> _logger;
        readonly UserManager<User> _userManager;
        readonly SignInManager<User> _signInManager;
        readonly RoleManager<Role> _roleManager;
        readonly IConfiguration _config;
        readonly JwtSettings _jwtSettings;

        public AuthService(IServices<RefreshToken> refreshToken, ILogger<AuthService> logger, UserManager<User> userManager, SignInManager<User> signInManager, RoleManager<Role> roleManager,
           IConfiguration config, IOptions<JwtSettings> jwtSettings)
        {
            _refreshToken = refreshToken;
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _config = config;
            _jwtSettings = jwtSettings.Value;
        }


        /// <summary>
        /// Authenticates a user using email and password.
        /// </summary>
        /// <param name="request">Login credentials (email and password).</param>
        /// <returns>
        /// An <see cref="AuthServiceResponse"/> indicating whether login succeeded,
        /// including JWT tokens if successful.
        /// </returns>
        public async Task<AuthServiceResponse> Login(LoginDto request)
        {
            _logger.LogInformation("Login attempt started for email: {Email}", request.Email);

            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
            {
                _logger.LogWarning("Login attempt for non-existent email: {Email}", request.Email);

                return new AuthServiceResponse
                {
                    IsSuccess = false,
                    Message = "Invalid Cedentials."
                };
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, true);

            if (result.IsLockedOut)
            {
                _logger.LogWarning("Account locked for user: {Email}", request.Email);

                return new AuthServiceResponse
                {
                    IsSuccess = false,
                    Message = "Account locked due to multiple failed login attempts."
                };
            }

            if (!result.Succeeded)
            {
                _logger.LogWarning("Invalid password for user: {Email}", request.Email);

                return new AuthServiceResponse
                {
                    IsSuccess = false,
                    Message = "Invalid Cedentials."
                };
            }

            _logger.LogInformation("Successful login for user: {Email}", request.Email);

            return new AuthServiceResponse
            {
                IsSuccess = true,
                Message = "Login successful.",
                AccessToken = await CreateAccessToken(user),
                RefreshToken = await CreateRefreshToken(user),
            };
        }

        /// <summary>
        /// Registers a new user using the provided registration data.
        /// </summary>
        /// <param name="request">User registration data.</param>
        /// <returns>
        /// An <see cref="AuthServiceResponse"/> indicating whether registration succeeded,
        /// including JWT tokens if successful.
        /// </returns>
        public async Task<AuthServiceResponse> Register(RegisterDto request)
        {
            _logger.LogInformation("Registration attempt started for email: {Email}", request.Email);

            var isUserExists = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == request.Email || u.PhoneNumber == request.PhoneNumber);

            if (isUserExists != null)
            {
                _logger.LogWarning("Registration attempt with existing email: {Email} or phone number: {Number}", request.Email, request.PhoneNumber);

                return new AuthServiceResponse
                {
                    IsSuccess = false,
                    Message = "Some of the provided information is already in use."
                };
            }

            if (request.Password != request.ConfirmPassword)
            {
                _logger.LogWarning("Password confirmation mismatch for email: {Email}", request.Email);

                return new AuthServiceResponse
                {
                    IsSuccess = false,
                    Message = "Passwords do not match."
                };
            }

            var newUser = new User
            {
                Fname = request.Fname,
                Lname = request.Lname,
                Email = request.Email,
                UserName = request.Email,
                PhoneNumber = request.PhoneNumber,
            };

            var result = await _userManager.CreateAsync(newUser, request.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("User registration failed for email {Email}: {Errors}", request.Email, errors);

                return new AuthServiceResponse
                {
                    IsSuccess = false,
                    Message = "User registration failed. " + errors
                };
            }

            await _userManager.AddToRoleAsync(newUser, RoleType.User.ToString());

            _logger.LogInformation("User registered successfully with email: {Email}", request.Email);

            return new AuthServiceResponse
            {
                IsSuccess = true,
                Message = "User registered successfully.",
                AccessToken = await CreateAccessToken(newUser),
                RefreshToken = await CreateRefreshToken(newUser),
            };
        }

        /// <summary>
        /// Generates a JWT access token for the specified user.
        /// The token includes user identity claims and assigned roles.
        /// </summary>
        /// <param name="user">The authenticated user.</param>
        /// <returns>A signed JWT access token.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when JWT configuration is missing or invalid.
        /// </exception>
        public async Task<string> CreateAccessToken(User user)
        {
            _logger.LogInformation("Creating access token for user: {UserId}", user.Id);

            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, $"{user.Fname} {user.Lname}")
            };

            var userRoles = await _userManager.GetRolesAsync(user);

            foreach (var role in userRoles)
            {
                userClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            if (string.IsNullOrEmpty(_jwtSettings.Key) || _jwtSettings.Key.Length < 32)
            {
                _logger.LogError("JWT key is invalid or too short");
                throw new InvalidOperationException("JWT configuration is invalid");
            }

            var authSecret = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));

            var tokenObject = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                expires: DateTime.Now.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                claims: userClaims,
                signingCredentials: new SigningCredentials(authSecret, SecurityAlgorithms.HmacSha256));

            string token = new JwtSecurityTokenHandler().WriteToken(tokenObject);
            _logger.LogInformation("Access token created successfully for user: {UserId}", user.Id);

            return token;
        }

        /// <summary>
        /// Generates a cryptographically secure refresh token,
        /// </summary>
        /// <param name="user">The user receiving the refresh token.</param>
        /// <returns>The generated refresh token string.</returns>
        public async Task<string> CreateRefreshToken(User user)
        {
            _logger.LogInformation("Creating refresh token for user: {UserId}", user.Id);

            string token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");

            RefreshToken? DbToken = _refreshToken.Query(rt => rt.UserId == user.Id)
                .OrderByDescending(rt => rt.Id)
                .FirstOrDefault();

            if (DbToken is not null)
            {
                DbToken.ExpDate = DateTime.Now;
            }

            RefreshToken refreshToken = new()
            {
                Token = token,
                ExpDate = DateTime.Now.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                UserId = user.Id
            };

            _logger.LogInformation("Refresh token id {0} for user: {1}", DbToken?.Id.ToString(), user.Id.ToString());

            await _refreshToken.AddAsync(refreshToken);
            await _refreshToken.SaveChangesAsync();

            _logger.LogInformation("Refresh token created and saved for user: {UserId}", user.Id);

            return token;
        }

        /// <summary>
        /// Validates a refresh token and issues new access and refresh tokens
        /// if the token exists and has not expired.
        /// </summary>
        /// <param name="token">The refresh token to validate.</param>
        /// <returns>
        /// An <see cref="AuthServiceResponse"/> containing new tokens if validation succeeds.
        /// </returns>
        public async Task<AuthServiceResponse> ValidateRefreshToken(string token)
        {
            _logger.LogInformation("Validating refresh token");

            var DBToken = await _refreshToken.GetAsync(t => t.Token == token, false, rf => rf.User);

            if (DBToken == null || DBToken.ExpDate < DateTime.Now)
            {
                _logger.LogWarning("Refresh token is invalid or expired.");

                return new AuthServiceResponse
                {
                    IsSuccess = false,
                    Message = "Refresh token is invalid or expired."
                };
            }

            _logger.LogInformation("Refresh token validated for user: {UserId}", DBToken.UserId);

            DBToken.ExpDate = DateTime.Now;
            await _refreshToken.SaveChangesAsync(); 

            return new AuthServiceResponse()
            {
                IsSuccess = true,
                Message = "succeeded",
                AccessToken = await CreateAccessToken(DBToken.User),
                RefreshToken = await CreateRefreshToken(DBToken.User)
            };
        }

        /// <summary>
        /// Assigns a role to an existing user if the user exists
        /// and does not already have the specified role.
        /// </summary>
        /// <param name="updatePermissionDto">Role assignment details.</param>
        /// <returns>
        /// An <see cref="AuthServiceResponse""" indicating the result of the role assignment.
        /// </returns>
        public async Task<AuthServiceResponse> AssignRoleAsync(UpdatePermissionDto updatePermissionDto)
        {
            int userId = updatePermissionDto.UserId;
            string role = updatePermissionDto.Role;

            _logger.LogInformation("Role assignment attempt: User={UserName}, Role={Role}", userId, role);

            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
            {
                _logger.LogWarning("Attempted to assign role to non-existent user: {UserName}", userId);

                return new AuthServiceResponse
                {
                    IsSuccess = false,
                    Message = "User not found"
                };
            }

            if (await _userManager.IsInRoleAsync(user, role))
            {
                return new AuthServiceResponse
                {
                    IsSuccess = false,
                    Message = $"User already has {role} role"
                };
            }

            var result = await _userManager.AddToRoleAsync(user, role);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));

                _logger.LogError("Failed to assign {Role} to user {UserName}: {Errors}",
                    role, userId, errors);

                return new AuthServiceResponse
                {
                    IsSuccess = false,
                    Message = "Failed to assign role: " + string.Join(", ", result.Errors.Select(e => e.Description))
                };
            }

            _logger.LogInformation("Assigned {Role} role to user: {UserName}", role, userId);

            return new AuthServiceResponse
            {
                IsSuccess = true,
                Message = $"User is now a {role}"
            };
        }

        /// <summary>
        /// Revokes the provided refresh token by marking it expired.
        /// </summary>
        /// <param name="refreshToken">Refresh token to revoke.</param>
        /// <returns>Result of the logout operation.</returns>
        public async Task<AuthServiceResponse> Logout(string refreshToken)
        {
            _logger.LogInformation("Logout attempt for refresh token");

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return new AuthServiceResponse
                {
                    IsSuccess = false,
                    Message = "Refresh token is required."
                };
            }

            var dbToken = await _refreshToken.GetAsync(t => t.Token == refreshToken, true, rf => rf.User);

            if (dbToken == null)
            {
                _logger.LogWarning("Refresh token not found during logout");
                return new AuthServiceResponse
                {
                    IsSuccess = false,
                    Message = "Refresh token not found."
                };
            }

            // Mark token expired instead of deleting — works with available Services API
            dbToken.ExpDate = DateTime.Now;
            await _refreshToken.SaveChangesAsync();

            _logger.LogInformation("Refresh token revoked for user: {UserId}", dbToken.UserId);

            return new AuthServiceResponse
            {
                IsSuccess = true,
                Message = "Logged out successfully."
            };
        }
    }
}
