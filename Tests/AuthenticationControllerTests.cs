using System.Net;
using System.Net.Http.Json;
using Application.DTOs;
using Application.Interfaces;
using Application.Responses;
using Core.Entities;
using Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Tests
{
    public partial class AdminEndpointsTests
    {
        [Theory]
        [InlineData("Login")]
        [InlineData("Register")]
        [InlineData("Refresh")]
        public async Task AuthenticationReturnsPublicUserInfoAndSetsCookiesWithoutExposingTokens(string action)
        {
            await SeedManagedAccounts();
            var service = Mock.Get(_host.Services.GetRequiredService<IAuthService>());
            var response = new AuthServiceResponse
            {
                IsSuccess = true, Message = "Authenticated.", AccessToken = "test-private-access", RefreshToken = "test-private-refresh"
            };
            service.Setup(s => s.Login(It.IsAny<LoginDto>())).ReturnsAsync(response);
            service.Setup(s => s.Register(It.IsAny<RegisterDto>())).ReturnsAsync(response);
            service.Setup(s => s.ValidateRefreshToken("old-refresh")).ReturnsAsync(response);
            if (action == "Refresh")
            {
                using var scope = _host.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.RefreshTokens.Add(new RefreshToken { UserId = 3, Token = "old-refresh", ExpDate = DateTime.Now.AddDays(1) });
                await db.SaveChangesAsync();
                _client.DefaultRequestHeaders.Add("Cookie", "refreshToken=old-refresh");
            }

            object payload = action == "Register"
                ? new RegisterDto { Fname = "Account3", Lname = "Test", Email = "account3@example.com", PhoneNumber = "01000000003", Password = "Secure1!", ConfirmPassword = "Secure1!" }
                : new LoginDto { Email = "account3@example.com", Password = "Secure1!" };
            var result = await _client.PostAsJsonAsync($"/api/Auth/{action}", payload);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var body = await result.Content.ReadFromJsonAsync<ApiResponse<UserInfoDto>>();
            Assert.True(body!.IsSuccess);
            Assert.Equal(3, body.Data!.UserId);
            Assert.Equal("AT", body.Data.UserName);
            Assert.Equal("User", Assert.Single(body.Data.Role));
            var cookies = result.Headers.GetValues("Set-Cookie").ToList();
            Assert.Equal(2, cookies.Count);
            Assert.Contains(cookies, c => c.StartsWith("accessToken=test-private-access"));
            Assert.Contains(cookies, c => c.StartsWith("refreshToken=test-private-refresh"));
            Assert.DoesNotContain("test-private", await result.Content.ReadAsStringAsync());
        }

        [Theory]
        [InlineData("Login", HttpStatusCode.BadRequest)]
        [InlineData("Refresh", HttpStatusCode.Unauthorized)]
        public async Task AuthenticationFailuresUseConsistentErrorsWithoutSettingCookies(string action, HttpStatusCode status)
        {
            var service = Mock.Get(_host.Services.GetRequiredService<IAuthService>());
            service.Setup(s => s.Login(It.IsAny<LoginDto>())).ReturnsAsync(new AuthServiceResponse { Message = "Rejected." });
            service.Setup(s => s.ValidateRefreshToken(It.IsAny<string>())).ReturnsAsync(new AuthServiceResponse { Message = "Rejected." });
            if (action == "Refresh")
                _client.DefaultRequestHeaders.Add("Cookie", "refreshToken=invalid");
            var result = await _client.PostAsJsonAsync($"/api/Auth/{action}", new LoginDto { Email = "test@example.com", Password = "wrong" });
            Assert.Equal(status, result.StatusCode);
            Assert.False(result.Headers.Contains("Set-Cookie"));
            var error = (await result.Content.ReadFromJsonAsync<ApiErrorResponse>())!;
            Assert.Equal((int)status, error.StatusCode);
            Assert.Equal($"/api/Auth/{action}", error.Path);
            Assert.Equal("POST", error.Method);
            Assert.False(string.IsNullOrWhiteSpace(error.TraceId));
        }

        [Fact]
        public async Task RefreshWithoutCookieReturnsConsistentUnauthorizedAndLogoutStillClearsCookies()
        {
            var response = await _client.PostAsync("/api/Auth/Refresh", null);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(401, (await response.Content.ReadFromJsonAsync<ApiErrorResponse>())!.StatusCode);
            response = await _client.PostAsync("/api/Auth/logout", null);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(2, response.Headers.GetValues("Set-Cookie").Count());
        }

        [Fact]
        public async Task UserInformationAndProfileUseTheSharedRequiredClaim()
        {
            await SeedManagedAccounts();
            UseAccountActor("User", 3);
            var auth = Mock.Get(_host.Services.GetRequiredService<IAuthService>());
            auth.Setup(s => s.GetUserInfoAsync("3")).ReturnsAsync(new ApiResponse<UserInfoDto>
            {
                IsSuccess = true, Data = new UserInfoDto { UserId = 3, UserName = "AT", Role = new() { "User" } }
            });
            var info = await _client.GetFromJsonAsync<ApiResponse<UserInfoDto>>("/api/Auth/UserInfo");
            Assert.Equal(3, info!.Data!.UserId);
            auth.Verify(s => s.GetUserInfoAsync("3"), Times.Once);
            var profile = await _client.GetFromJsonAsync<ApiResponse<UserProfileDto>>("/api/Users/profile");
            Assert.Equal("account3@example.com", profile!.Data!.Email);
        }
    }
}
