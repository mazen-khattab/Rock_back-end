using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using API.Controllers;
using Application.DTOs;
using Application.Interfaces;
using Application.Responses;
using Application.Services;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Infrastructure.DataSeeding;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Tests
{
    public partial class AdminEndpointsTests : IAsyncLifetime
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");
        private IHost _host = null!;
        private HttpClient _client = null!;

        public async Task InitializeAsync()
        {
            await _connection.OpenAsync();
            // Use real controllers and services without running production startup or migrations.
            _host = await new HostBuilder().ConfigureWebHost(web => web.UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddLogging();
                    services.AddScoped<AppDbContext>(_ => new TestDbContext(
                        new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options));
                    services.AddScoped(typeof(IRepo<>), typeof(Repo<>));
                    services.AddScoped<IUnitOfWork, UnitOfWork>();
                    services.AddScoped(typeof(IServices<>), typeof(Services<>));
                    services.AddScoped<ICategoryService, CategoryService>();
                    services.AddScoped<IColorService, ColorService>();
                    services.AddScoped<ISizeService, SizeService>();
                    services.AddScoped<IOrderService, OrderService>();
                    services.AddScoped<IAccountRepository, AccountRepository>();
                    services.AddScoped<IAccountManagementService, AccountManagementService>();
                    services.AddScoped<IUserServices, UserServices>();
                    services.AddSingleton(Mock.Of<ICartService>());
                    services.AddSingleton(Mock.Of<IAuthService>());
                    services.AddIdentityCore<User>(options =>
                    {
                        options.Password.RequiredLength = 4;
                        options.Password.RequireNonAlphanumeric = true;
                    }).AddRoles<Role>().AddEntityFrameworkStores<AppDbContext>();
                    services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
                    services.AddAuthorization();
                    services.AddControllers().AddApplicationPart(typeof(OrdersController).Assembly);
                })
                .Configure(app =>
                {
                    app.UseRequestLocalization(new RequestLocalizationOptions()
                        .SetDefaultCulture("ar").AddSupportedCultures("ar", "en").AddSupportedUICultures("ar", "en"));
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                })).StartAsync();

            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();
            // Nonstandard IDs catch code that assumes Arabic and English have IDs 1 and 2.
            db.Languages.AddRange(new Language { Id = 7, Code = "ar", Name = "Arabic" },
                new Language { Id = 9, Code = "en", Name = "English" });
            await db.SaveChangesAsync();
            _client = _host.GetTestClient();
            _client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
        }

        public async Task DisposeAsync()
        {
            _client?.Dispose();
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }
            await _connection.DisposeAsync();
        }

        [Theory]
        [InlineData("Categories")]
        [InlineData("Colors")]
        public async Task CreateTranslatedLookupPersistsBothLanguages(string resource)
        {
            var response = await _client.PostAsJsonAsync($"/api/{resource}/admin/create",
                new { nameAr = " عربي ", nameEn = " English ", hexCode = "#aabbcc", isActive = true });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = await ReadJson(response);
            int id = body.GetProperty("data").GetProperty("id").GetInt32();
            Assert.True(id > 0);
            Assert.Equal("عربي", body.GetProperty("data").GetProperty("nameAr").GetString());
            Assert.Equal("English", body.GetProperty("data").GetProperty("nameEn").GetString());

            // Follow Location through new requests so translations must be loaded from storage.
            _client.DefaultRequestHeaders.Add("Accept-Language", "en");
            var details = await ReadJson(await _client.GetAsync(response.Headers.Location));
            AssertBilingualNames(details.GetProperty("data"), "عربي", "English");
            _client.DefaultRequestHeaders.Remove("Accept-Language");
            details = await ReadJson(await _client.GetAsync(response.Headers.Location));
            AssertBilingualNames(details.GetProperty("data"), "عربي", "English");
            Assert.True(details.GetProperty("data").GetProperty("isActive").GetBoolean());

            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (resource == "Categories")
                Assert.Equal(2, await db.CategoryTranslations.CountAsync(t => t.CategoryId == id));
            else
            {
                Assert.Equal(2, await db.ColorTranslations.CountAsync(t => t.ColorId == id));
                Assert.Equal("#AABBCC", (await db.Colors.FindAsync(id))!.HexCode);
            }
        }

        [Theory]
        [InlineData("Categories")]
        [InlineData("Colors")]
        public async Task MissingLanguageDoesNotPartiallyCreateLookup(string resource)
        {
            using (var scope = _host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Languages.Remove(await db.Languages.SingleAsync(l => l.Code == "en"));
                await db.SaveChangesAsync();
            }

            var response = await _client.PostAsJsonAsync($"/api/{resource}/admin/create",
                new { nameAr = "عربي", nameEn = "English", hexCode = "#AABBCC", isActive = true });
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
            Assert.False(error!.IsSuccess);
            Assert.Equal(404, error.StatusCode);
            Assert.Equal("POST", error.Method);
            Assert.False(string.IsNullOrWhiteSpace(error.TraceId));

            using var checkScope = _host.Services.CreateScope();
            var checkDb = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(0, await checkDb.Categories.CountAsync());
            Assert.Equal(0, await checkDb.Colors.CountAsync());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CreateSizePreservesSortOrderAndActiveState(bool isActive)
        {
            _client.DefaultRequestHeaders.Remove("X-Test-Role");
            _client.DefaultRequestHeaders.Add("X-Test-Role", "Owner");
            var response = await _client.PostAsJsonAsync("/api/Sizes/admin/create",
                new SizeDetailsDto { Name = " XL ", SortOrder = 4, IsActive = isActive });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var created = await response.Content.ReadFromJsonAsync<ApiResponse<SizeDto>>();
            Assert.True(created!.Data!.Id > 0);
            Assert.Equal("XL", created.Data.Name);
            Assert.Equal(4, created.Data.SortOrder);
            Assert.Equal(isActive, created.Data.IsActive);
            var details = await _client.GetFromJsonAsync<ApiResponse<SizeDetailsDto>>(response.Headers.Location);
            Assert.Equal(isActive, details!.Data!.IsActive);
        }

        [Theory]
        [InlineData("Categories", "{\"nameAr\":\" \",\"nameEn\":\"English\"}")]
        [InlineData("Categories", "{\"nameAr\":\"Arabic\"}")]
        [InlineData("Colors", "{\"nameAr\":\"Arabic\",\"nameEn\":\"English\",\"hexCode\":\"red\"}")]
        [InlineData("Colors", "{\"nameAr\":\"Arabic\",\"nameEn\":\"English\",\"hexCode\":\"#1234567\"}")]
        [InlineData("Sizes", "{\"name\":\" \"}")]
        public async Task InvalidCreateRequestsAreRejectedWithoutWrites(string resource, string json)
        {
            var response = await _client.PostAsync($"/api/{resource}/admin/create",
                new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(0, await db.Categories.CountAsync());
            Assert.Equal(0, await db.Colors.CountAsync());
            Assert.Equal(0, await db.Sizes.CountAsync());
        }

        [Theory]
        [InlineData("POST", "/api/Categories/admin/create")]
        [InlineData("POST", "/api/Colors/admin/create")]
        [InlineData("POST", "/api/Sizes/admin/create")]
        [InlineData("GET", "/api/Orders/admin")]
        [InlineData("GET", "/api/Orders/admin/1")]
        public async Task AdminEndpointsRejectAnonymousAndRegularUsers(string method, string path)
        {
            _client.DefaultRequestHeaders.Remove("X-Test-Role");
            Assert.Equal(HttpStatusCode.Unauthorized, (await Send(method, path)).StatusCode);
            _client.DefaultRequestHeaders.Add("X-Test-Role", "User");
            Assert.Equal(HttpStatusCode.Forbidden, (await Send(method, path)).StatusCode);
        }

        [Fact]
        public async Task OrdersArePagedAcrossCustomersAndExcludeDeletedOrders()
        {
            using (var scope = _host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var first = new User { Fname = "First", Lname = "Customer", UserName = "first" };
                var deletedCustomer = new User { Fname = "Deleted", Lname = "Customer", UserName = "deleted", IsDeleted = true };
                db.Orders.AddRange(
                    new Order { Id = 1, Number = "ORD-1", User = first, City = "Cairo" },
                    new Order { Id = 2, Number = "ORD-2", User = deletedCustomer, City = "Giza" },
                    new Order { Id = 3, Number = "ORD-3", User = first, IsDeleted = true });
                await db.SaveChangesAsync();
                // Force a timestamp tie to verify stable ordering by ID.
                await db.Orders.IgnoreQueryFilters().ExecuteUpdateAsync(o => o.SetProperty(x => x.CreatedAt, new DateTime(2026, 1, 1)));
            }

            var page = await ReadJson(await _client.GetAsync("/api/Orders/admin?pageNumber=1&pageSize=1"));
            Assert.Equal(2, page.GetProperty("totalCount").GetInt32());
            Assert.Equal(2, page.GetProperty("totalPages").GetInt32());
            var item = Assert.Single(page.GetProperty("items").EnumerateArray());
            Assert.Equal("ORD-2", item.GetProperty("orderNumber").GetString());
            Assert.Equal("Deleted Customer", item.GetProperty("customerName").GetString());
            Assert.Equal("Giza", item.GetProperty("city").GetString());
            page = await ReadJson(await _client.GetAsync("/api/Orders/admin?pageNumber=2&pageSize=1"));
            Assert.Equal("ORD-1", Assert.Single(page.GetProperty("items").EnumerateArray()).GetProperty("orderNumber").GetString());
            page = await ReadJson(await _client.GetAsync("/api/Orders/admin?pageNumber=3&pageSize=1"));
            Assert.Empty(page.GetProperty("items").EnumerateArray());
            Assert.Equal(2, page.GetProperty("totalCount").GetInt32());
        }

        [Theory]
        [InlineData("pageNumber=0")]
        [InlineData("pageSize=0")]
        [InlineData("pageSize=101")]
        [InlineData("pageNumber=-1")]
        public async Task InvalidOrderPaginationIsRejected(string query)
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync($"/api/Orders/admin?{query}")).StatusCode);
        }

        [Theory]
        [InlineData("en", "Shirt", "Blue")]
        [InlineData("ar", "قميص", "أزرق")]
        public async Task OrderDetailsKeepCheckoutAmountsAndHistoricalItems(string language, string productName, string colorName)
        {
            await SeedDetailedOrder();
            _client.DefaultRequestHeaders.Add("Accept-Language", language);
            var response = await _client.GetAsync("/api/Orders/admin/10");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var order = (await response.Content.ReadFromJsonAsync<ApiResponse<OrderDetailsAdminDto>>())!.Data!;
            Assert.Equal("Buyer Name", order.CustomerName);
            Assert.Equal("buyer@example.com", order.Email);
            Assert.Equal("01000000000", order.Phone);
            Assert.Equal("Checkout address", order.FullAddress);
            Assert.Equal("Cairo", order.City);
            Assert.Equal("Pending", order.Status);
            Assert.Equal(150m, order.TotalPrice);
            var item = Assert.Single(order.OrderDetails);
            Assert.Equal(productName, item.Name);
            Assert.Equal(colorName, item.ColorName);
            Assert.Equal("XL", item.SizeName);
            Assert.Equal(2, item.Quantity);
            Assert.Equal(80m, item.UnitPrice);
            Assert.Equal(10m, item.Discount);
            Assert.Equal(150m, item.TotalPrice);
            Assert.EndsWith("/Images/first.jpg", item.Image);
        }

        [Fact]
        public async Task MissingAndDeletedOrdersReturnNotFound()
        {
            await SeedDetailedOrder();
            Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/Orders/admin/999")).StatusCode);
            using (var scope = _host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Orders.Remove(await db.Orders.SingleAsync());
                await db.SaveChangesAsync();
            }
            var response = await _client.GetAsync("/api/Orders/admin/10");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var error = (await response.Content.ReadFromJsonAsync<ApiErrorResponse>())!;
            Assert.Equal("Order not found.", error.Message);
            Assert.Equal("/api/Orders/admin/10", error.Path);
            Assert.Equal("GET", error.Method);
            Assert.False(string.IsNullOrWhiteSpace(error.TraceId));
        }

        [Fact]
        public async Task LanguageSeedingCreatesBothLanguagesAndCanRunAgainWithoutChanges()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Languages.ExecuteDeleteAsync();

            var startedAt = DateTime.Now;
            await LanguageSeeder.SeedAsync(db, NullLogger.Instance);
            var languages = await db.Languages.AsNoTracking().OrderBy(l => l.Code).ToListAsync();

            Assert.Equal(2, languages.Count);
            Assert.Equal("ar", languages[0].Code);
            Assert.Equal("Arabic", languages[0].Name);
            Assert.Equal("en", languages[1].Code);
            Assert.Equal("English", languages[1].Name);
            Assert.All(languages, l => Assert.InRange(l.CreatedAt, startedAt, DateTime.Now));

            await LanguageSeeder.SeedAsync(db, NullLogger.Instance);
            var repeated = await db.Languages.AsNoTracking().OrderBy(l => l.Code).ToListAsync();
            Assert.Equal(languages.Select(l => (l.Id, l.Name, l.Code, l.CreatedAt)),
                repeated.Select(l => (l.Id, l.Name, l.Code, l.CreatedAt)));
        }

        [Fact]
        public async Task LanguageSeedingAddsMissingLanguageAndPreservesExistingRecordRegardlessOfCodeCase()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Languages.Where(l => l.Code == "ar").ExecuteDeleteAsync();
            var originalDate = new DateTime(2025, 1, 1);
            await db.Languages.ExecuteUpdateAsync(l => l
                .SetProperty(x => x.Code, "EN")
                .SetProperty(x => x.Name, "Existing English")
                .SetProperty(x => x.CreatedAt, originalDate));

            await LanguageSeeder.SeedAsync(db, NullLogger.Instance);

            Assert.Equal(2, await db.Languages.CountAsync());
            var english = await db.Languages.SingleAsync(l => l.Code == "EN");
            Assert.Equal(9, english.Id);
            Assert.Equal("Existing English", english.Name);
            Assert.Equal(originalDate, english.CreatedAt);
            Assert.Equal("Arabic", (await db.Languages.SingleAsync(l => l.Code == "ar")).Name);
        }

        private async Task SeedDetailedOrder()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var product = new Product
            {
                Category = new Category(), Price = 999m, OriginalPrice = 1200m, IsDeleted = true,
                Translations = new List<ProductTranslation>
                {
                    new() { LanguageId = 7, Name = "قميص", Slug = "shirt-ar" },
                    new() { LanguageId = 9, Name = "Shirt", Slug = "shirt-en" }
                }
            };
            var variant = new Variant
            {
                Product = product,
                Color = new Color
                {
                    HexCode = "#0000FF", Translations = new List<ColorTranslation>
                    {
                        new() { LanguageId = 7, Name = "أزرق" },
                        new() { LanguageId = 9, Name = "Blue" }
                    }
                },
                Size = new Size { Name = "XL" },
                Images = new List<VariantImage>
                {
                    new() { SortOrder = 2, MediaAsset = new MediaAsset { FilePath = "Images/second.jpg" } },
                    new() { SortOrder = 1, MediaAsset = new MediaAsset { FilePath = "Images/first.jpg" } }
                }
            };
            db.Orders.Add(new Order
            {
                Id = 10, Number = "ORD-10", TotalPrice = 150m, City = "Cairo", FullAddress = "Checkout address",
                Status = OrderStatus.Pending,
                User = new User
                {
                    UserName = "buyer", Fname = "Buyer", Lname = "Name", Email = "buyer@example.com",
                    PhoneNumber = "01000000000", FullAddress = "Changed address", IsDeleted = true
                },
                OrderDetails = new List<OrderDetail>
                {
                    new() { Variant = variant, Quantity = 2, UnitPrice = 80m, Discount = 10m, TotalPrice = 150m }
                }
            });
            await db.SaveChangesAsync();
        }

        private Task<HttpResponseMessage> Send(string method, string path) =>
            _client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path) { Content = JsonContent.Create(new { }) });

        private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        private sealed class TestDbContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
        {
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                // SQLite accepts TEXT in place of SQL Server's nvarchar(max).
                foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetProperties()))
                {
                    if (property.GetColumnType() == "nvarchar(max)")
                        property.SetColumnType("TEXT");
                }
                // SQLite needs a default for SQL Server's generated rowversion column.
                modelBuilder.Entity<Variant>().Property(v => v.RowVersion).HasDefaultValueSql("randomblob(8)");
            }
        }

        private sealed class TestAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
            : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
        {
            protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            {
                if (!Request.Headers.TryGetValue("X-Test-Role", out var role))
                    return Task.FromResult(AuthenticateResult.NoResult());

                var identity = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, Request.Headers["X-Test-UserId"].FirstOrDefault() ?? "1"),
                    new Claim(ClaimTypes.Role, role.ToString())
                }, Scheme.Name);
                return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
            }
        }
    }
}
