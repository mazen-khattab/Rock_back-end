using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using API.Extensions;
using Application.DTOs;
using Core.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests
{
    public partial class AdminEndpointsTests
    {
        [Theory]
        [InlineData("GET", "/api/Users/admin")]
        [InlineData("GET", "/api/Users/admin/2")]
        [InlineData("POST", "/api/Users/admin/create")]
        [InlineData("PUT", "/api/Users/admin/2/edit")]
        [InlineData("DELETE", "/api/Users/admin/2/delete")]
        [InlineData("GET", "/api/Customers")]
        [InlineData("GET", "/api/Customers/3")]
        [InlineData("POST", "/api/Customers/create")]
        [InlineData("PUT", "/api/Customers/3/edit")]
        [InlineData("DELETE", "/api/Customers/3/delete")]
        public async Task AccountManagementRejectsAnonymousAndCustomers(string method, string path)
        {
            _client.DefaultRequestHeaders.Remove("X-Test-Role");
            Assert.Equal(HttpStatusCode.Unauthorized, (await Send(method, path)).StatusCode);
            SetRole("User");
            Assert.Equal(HttpStatusCode.Forbidden, (await Send(method, path)).StatusCode);
        }

        [Theory]
        [InlineData("POST", "/api/Users/admin/create")]
        [InlineData("PUT", "/api/Users/admin/2/edit")]
        [InlineData("DELETE", "/api/Users/admin/2/delete")]
        [InlineData("POST", "/api/Auth/assign-role")]
        public async Task AdminCannotModifyStaffOrAssignPrivilegedRoles(string method, string path)
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await Send(method, path)).StatusCode);
        }

        [Fact]
        public async Task OwnerCustomerAccessMatchesCurrentControllerAndActorPermissions()
        {
            await SeedManagedAccounts();
            UseAccountActor("Owner", 4);
            // The reference controller currently allows Owner reads; the service still checks Admin for writes.
            Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/Customers")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/Customers/3")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,
                (await _client.PostAsJsonAsync("/api/Customers/create", AccountCreatePayload(false, "owner-test@example.com"))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,
                (await _client.PutAsJsonAsync("/api/Customers/3/edit", AccountUpdatePayload())).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await _client.DeleteAsync("/api/Customers/3/delete")).StatusCode);
        }

        [Theory]
        [InlineData("Admin", 2)]
        [InlineData("Owner", 1)]
        public async Task StaffListsAndDetailsSeparateAccountGroupsAndHideIdentitySecrets(string role, int actorId)
        {
            await SeedManagedAccounts();
            UseAccountActor(role, actorId);
            var staff = await ReadJson(await _client.GetAsync("/api/Users/admin?pageSize=2"));
            Assert.Equal(3, staff.GetProperty("totalCount").GetInt32());
            Assert.Equal(2, staff.GetProperty("items").GetArrayLength());
            Assert.Equal(2, staff.GetProperty("totalPages").GetInt32());
            var detailResponse = await _client.GetAsync("/api/Users/admin/1");
            var details = await ReadJson(detailResponse);
            Assert.Equal(1, details.GetProperty("data").GetProperty("id").GetInt32());
            Assert.Equal(3, details.GetProperty("data").GetProperty("roles").GetArrayLength());
            string json = await detailResponse.Content.ReadAsStringAsync();
            Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("securityStamp", json);
            Assert.DoesNotContain("concurrencyStamp", json);
            Assert.DoesNotContain("refreshToken", json);
            Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/Users/admin/3")).StatusCode);
        }

        [Fact]
        public async Task CustomerListExcludesMixedRoleOwnersDeletedAccountsAndRolelessAccounts()
        {
            await SeedManagedAccounts();
            UseAccountActor("Admin", 2);
            var list = await ReadJson(await _client.GetAsync("/api/Customers"));
            var customer = Assert.Single(list.GetProperty("items").EnumerateArray());
            Assert.Equal(3, customer.GetProperty("id").GetInt32());
            var filtered = await ReadJson(await _client.GetAsync("/api/Customers?search=not-present"));
            Assert.Empty(filtered.GetProperty("items").EnumerateArray());
            Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/Customers/1")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/Customers/2")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/Customers/5")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync("/api/Customers?pageSize=101")).StatusCode);
        }

        [Theory]
        [InlineData(true, "Admin")]
        [InlineData(true, "Owner")]
        [InlineData(false, "User")]
        public async Task CreateManagedAccountHashesPasswordAndAssignsOnlyRequestedAccountRoles(bool staff, string role)
        {
            await SeedManagedAccounts();
            UseAccountActor(staff ? "Owner" : "Admin", staff ? 1 : 2);
            var response = await _client.PostAsJsonAsync(staff ? "/api/Users/admin/create" : "/api/Customers/create",
                AccountCreatePayload(staff, "new@example.com", role));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.False(response.Headers.Contains("Set-Cookie"));
            var body = await ReadJson(response);
            int id = body.GetProperty("data").GetProperty("id").GetInt32();
            Assert.Equal(role, Assert.Single(body.GetProperty("data").GetProperty("roles").EnumerateArray()).GetString());
            Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync(response.Headers.Location)).StatusCode);

            using var scope = _host.Services.CreateScope();
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = (await manager.FindByIdAsync(id.ToString()))!;
            Assert.NotEqual("Secure1!", user.PasswordHash);
            Assert.True(await manager.CheckPasswordAsync(user, "Secure1!"));
            Assert.Equal("new@example.com", user.NormalizedEmail!.ToLowerInvariant());
            Assert.DoesNotContain("Secure1!", await response.Content.ReadAsStringAsync());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task InvalidOrDuplicateAccountCreationDoesNotLeavePartialAccounts(bool staff)
        {
            await SeedManagedAccounts();
            UseAccountActor(staff ? "Owner" : "Admin", staff ? 1 : 2);
            string path = staff ? "/api/Users/admin/create" : "/api/Customers/create";
            var duplicate = await _client.PostAsJsonAsync(path, AccountCreatePayload(staff, "account3@example.com"));
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
            var invalid = await _client.PostAsJsonAsync(path, AccountCreatePayload(staff, "weak@example.com", password: "weak"));
            Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(6, await db.Users.IgnoreQueryFilters().CountAsync());
            Assert.False(await db.Users.AnyAsync(u => u.Email == "weak@example.com"));
        }

        [Fact]
        public async Task CustomerRequestsCannotAssignRolesOrModifyIdentityInternals()
        {
            await SeedManagedAccounts();
            UseAccountActor("Admin", 2);
            var injected = new
            {
                firstName = "Injected", lastName = "Customer", email = "inject@example.com", phone = "01099999999",
                password = "Secure1!", confirmPassword = "Secure1!", roles = new[] { "Owner" }
            };
            Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync("/api/Customers/create", injected)).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest,
                (await _client.PutAsJsonAsync("/api/Customers/3/edit", new
                {
                    firstName = "Injected", lastName = "Customer", email = "account3@example.com", phone = "01000000003",
                    isDeleted = true
                })).StatusCode);
            UseAccountActor("Owner", 1);
            Assert.Equal(HttpStatusCode.UnprocessableEntity,
                (await _client.PostAsJsonAsync("/api/Users/admin/create", AccountCreatePayload(true, "badrole@example.com", "User"))).StatusCode);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task UpdateAccountPersistsProfileAndNormalizedEmailWithoutTouchingOtherAccountGroup(bool staff)
        {
            await SeedManagedAccounts();
            UseAccountActor(staff ? "Owner" : "Admin", staff ? 1 : 2);
            int target = staff ? 2 : 3;
            string prefix = staff ? "/api/Users/admin" : "/api/Customers";
            var response = await _client.PutAsJsonAsync($"{prefix}/{target}/edit", AccountUpdatePayload());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = (await ReadJson(response)).GetProperty("data");
            Assert.Equal("Updated", data.GetProperty("firstName").GetString());
            Assert.Equal("Cairo", data.GetProperty("city").GetString());
            Assert.Equal("updated@example.com", data.GetProperty("email").GetString());
            Assert.Equal(staff ? "Admin" : "User", Assert.Single(data.GetProperty("roles").EnumerateArray()).GetString());
            using var scope = _host.Services.CreateScope();
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = (await manager.FindByIdAsync(target.ToString()))!;
            Assert.Equal("UPDATED@EXAMPLE.COM", user.NormalizedEmail);
            Assert.Equal("updated@example.com", user.UserName);
            Assert.NotNull(user.UpdatedAt);

            int wrongGroup = staff ? 3 : 1;
            Assert.Equal(HttpStatusCode.NotFound,
                (await _client.PutAsJsonAsync($"{prefix}/{wrongGroup}/edit", AccountUpdatePayload())).StatusCode);
        }

        [Fact]
        public async Task StaffRolesCanBeChangedRepeatedlyAndRefreshTokensAreRevoked()
        {
            await SeedManagedAccounts();
            UseAccountActor("Owner", 1);
            using (var scope = _host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.RefreshTokens.Add(new RefreshToken { UserId = 2, Token = "old-refresh", ExpDate = DateTime.Now.AddDays(1) });
                await db.SaveChangesAsync();
            }

            foreach (string role in new[] { "Owner", "Admin", "Owner", "Admin" })
            {
                var response = await _client.PutAsJsonAsync("/api/Users/admin/2/edit", AccountUpdatePayload(new[] { role }));
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(role, Assert.Single((await ReadJson(response)).GetProperty("data").GetProperty("roles").EnumerateArray()).GetString());
            }
            using var checkScope = _host.Services.CreateScope();
            var checkDb = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.False(await checkDb.RefreshTokens.AnyAsync(t => t.UserId == 2));
            Assert.Equal(2, await checkDb.UserRoles.IgnoreQueryFilters().CountAsync(ur => ur.UserId == 2));
        }

        [Fact]
        public async Task OwnerCannotDeleteSelfOrRemoveOwnOwnerRole()
        {
            await SeedManagedAccounts();
            UseAccountActor("Owner", 1);
            Assert.Equal(HttpStatusCode.Conflict, (await _client.DeleteAsync("/api/Users/admin/1/delete")).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict,
                (await _client.PutAsJsonAsync("/api/Users/admin/1/edit", AccountUpdatePayload(new[] { "Admin" }))).StatusCode);
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(true, true)]
        [InlineData(false, false)]
        [InlineData(false, true)]
        public async Task AnyOrderPreventsDeletionWithoutChangingAccountOrRoles(bool staff, bool deletedOrder)
        {
            await SeedManagedAccounts();
            UseAccountActor(staff ? "Owner" : "Admin", staff ? 1 : 2);
            int target = staff ? 2 : 3;
            using (var scope = _host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Orders.Add(new Order { UserId = target, Number = "KEEP-ORDER", IsDeleted = deletedOrder });
                await db.SaveChangesAsync();
            }
            var response = await _client.DeleteAsync(staff ? $"/api/Users/admin/{target}/delete" : $"/api/Customers/{target}/delete");
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Contains("orders", await response.Content.ReadAsStringAsync());

            using var checkScope = _host.Services.CreateScope();
            var checkDb = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await checkDb.Users.IgnoreQueryFilters().SingleAsync(u => u.Id == target);
            Assert.False(user.IsDeleted);
            Assert.Null(user.DeletedAt);
            Assert.Equal(1, await checkDb.Orders.IgnoreQueryFilters().CountAsync(o => o.UserId == target));
            Assert.Equal(1, await checkDb.UserRoles.CountAsync(ur => ur.UserId == target));
        }

        [Theory]
        [InlineData(true, 2)]
        [InlineData(true, 4)]
        [InlineData(false, 3)]
        public async Task OrderlessAccountIsPermanentlyDeletedWithItsIdentityLinksAndTokens(bool staff, int target)
        {
            await SeedManagedAccounts();
            UseAccountActor(staff ? "Owner" : "Admin", staff ? 1 : 2);
            using (var scope = _host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.RefreshTokens.Add(new RefreshToken { UserId = target, Token = "remove-refresh", ExpDate = DateTime.Now.AddDays(1) });
                db.UserClaims.Add(new IdentityUserClaim<int> { UserId = target, ClaimType = "test", ClaimValue = "remove-claim" });
                await db.SaveChangesAsync();
            }
            string path = staff ? $"/api/Users/admin/{target}/delete" : $"/api/Customers/{target}/delete";
            Assert.Equal(HttpStatusCode.OK, (await _client.DeleteAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync(path)).StatusCode);
            using var checkScope = _host.Services.CreateScope();
            var checkDb = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.False(await checkDb.Users.IgnoreQueryFilters().AnyAsync(u => u.Id == target));
            Assert.False(await checkDb.UserRoles.IgnoreQueryFilters().AnyAsync(ur => ur.UserId == target));
            Assert.False(await checkDb.RefreshTokens.AnyAsync(t => t.UserId == target));
            Assert.False(await checkDb.UserClaims.AnyAsync(c => c.UserId == target));
        }

        [Fact]
        public async Task CustomerEndpointCannotDeleteMixedRoleOwnerAndDatabaseActorRoleIsRechecked()
        {
            await SeedManagedAccounts();
            UseAccountActor("Admin", 2);
            Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync("/api/Customers/1/delete")).StatusCode);
            UseAccountActor("Admin", 3);
            Assert.Equal(HttpStatusCode.Forbidden, (await _client.DeleteAsync("/api/Customers/3/delete")).StatusCode);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task JwtValidationRejectsDeletedAccountsAndStaleRoleClaims(bool deletedAccount)
        {
            await SeedManagedAccounts();
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "2"), new Claim(ClaimTypes.Role, "Admin")
            }, "Bearer"));
            var http = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
            var scheme = new AuthenticationScheme("Bearer", "Bearer", typeof(JwtBearerHandler));
            var valid = new TokenValidatedContext(http, scheme, new JwtBearerOptions()) { Principal = principal };
            await AccountTokenValidation.ValidateAsync(valid);
            Assert.Null(valid.Result);
            if (deletedAccount)
                await db.Users.Where(u => u.Id == 2).ExecuteDeleteAsync();
            else
                await db.UserRoles.Where(ur => ur.UserId == 2).ExecuteUpdateAsync(ur => ur.SetProperty(x => x.IsDeleted, true));

            var invalid = new TokenValidatedContext(http, scheme, new JwtBearerOptions()) { Principal = principal };
            await AccountTokenValidation.ValidateAsync(invalid);
            Assert.NotNull(invalid.Result?.Failure);
        }

        private async Task SeedManagedAccounts()
        {
            using var scope = _host.Services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            foreach (string role in new[] { "User", "Admin", "Owner" })
                Assert.True((await roleManager.CreateAsync(new Role { Name = role })).Succeeded);
            for (int id = 1; id <= 6; id++)
            {
                var user = new User
                {
                    Id = id, Fname = $"Account{id}", Lname = "Test", Email = $"account{id}@example.com",
                    UserName = $"account{id}@example.com", PhoneNumber = $"0100000000{id}"
                };
                Assert.True((await manager.CreateAsync(user, "Secure1!")).Succeeded);
                string[] roles = id switch
                {
                    1 => new[] { "User", "Admin", "Owner" }, 2 => new[] { "Admin" },
                    3 or 5 => new[] { "User" }, 4 => new[] { "Owner" }, _ => Array.Empty<string>()
                };
                if (roles.Length > 0)
                    Assert.True((await manager.AddToRolesAsync(user, roles)).Succeeded);
            }
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Users.Where(u => u.Id == 5).ExecuteUpdateAsync(u => u.SetProperty(x => x.IsDeleted, true));
        }

        private void UseAccountActor(string role, int id)
        {
            SetRole(role);
            _client.DefaultRequestHeaders.Remove("X-Test-UserId");
            _client.DefaultRequestHeaders.Add("X-Test-UserId", id.ToString());
        }

        private static object AccountCreatePayload(bool staff, string email, string role = "Admin", string password = "Secure1!") =>
            staff ? new CreateStaffDto
            {
                FirstName = "New", LastName = "Staff", Email = email, Phone = "01099999999",
                Password = password, ConfirmPassword = password, Roles = new() { role }
            } : new CreateCustomerDto
            {
                FirstName = "New", LastName = "Customer", Email = email, Phone = "01099999999",
                Password = password, ConfirmPassword = password
            };

        private static object AccountUpdatePayload(string[]? roles = null) => roles == null
            ? new AccountProfileRequestDto { FirstName = "Updated", LastName = "Account", Email = "updated@example.com", Phone = "01099999999", City = "Cairo" }
            : new UpdateStaffDto { FirstName = "Updated", LastName = "Account", Email = "updated@example.com", Phone = "01099999999", City = "Cairo", Roles = roles.ToList() };
    }
}
