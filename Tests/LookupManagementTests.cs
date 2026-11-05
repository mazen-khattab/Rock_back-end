using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs;
using Application.Responses;
using Core.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests
{
    public partial class AdminEndpointsTests
    {
        [Theory]
        [InlineData("Categories", "Admin")]
        [InlineData("Categories", "Owner")]
        [InlineData("Colors", "Admin")]
        [InlineData("Colors", "Owner")]
        [InlineData("Sizes", "Admin")]
        [InlineData("Sizes", "Owner")]
        public async Task UpdateLookupPersistsEditableFieldsAndPreservesAuditFields(string resource, string role)
        {
            int id = await CreateLookup(resource);
            DateTime createdAt;
            using (var scope = _host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                createdAt = await LookupCreatedAt(db, resource, id);
            }

            SetRole(role);
            var payload = JsonSerializer.SerializeToNode(LookupPayload(resource, updated: true, id),
                new JsonSerializerOptions(JsonSerializerDefaults.Web))!.AsObject();
            // Audit fields supplied by a client must never be bound to the entity.
            payload["createdAt"] = "2000-01-01T00:00:00";
            payload["updatedAt"] = "2000-01-01T00:00:00";
            var response = await _client.PutAsJsonAsync($"/api/{resource}/admin/{id}/edit", payload);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJson(response);
            Assert.Equal(id, body.GetProperty("data").GetProperty("id").GetInt32());
            Assert.False(body.GetProperty("data").GetProperty("isActive").GetBoolean());

            if (resource == "Sizes")
            {
                Assert.Equal("XL", body.GetProperty("data").GetProperty("name").GetString());
                Assert.Equal(8, body.GetProperty("data").GetProperty("sortOrder").GetInt32());
            }
            else
            {
                Assert.Equal("Updated Arabic", body.GetProperty("data").GetProperty("nameAr").GetString());
                Assert.Equal("Updated English", body.GetProperty("data").GetProperty("nameEn").GetString());
                if (resource == "Colors")
                    Assert.Equal("#AABBCC", body.GetProperty("data").GetProperty("hexCode").GetString());
            }

            // Read through a separate request to verify persistence, not only an in-memory DTO.
            var details = await ReadJson(await _client.GetAsync($"/api/{resource}/{id}"));
            Assert.Equal(resource == "Sizes" ? "XL" : "Updated Arabic", details.GetProperty("data").GetProperty("name").GetString());
            Assert.False(details.GetProperty("data").GetProperty("isActive").GetBoolean());
            using var checkScope = _host.Services.CreateScope();
            var checkDb = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(createdAt, await LookupCreatedAt(checkDb, resource, id));
            Assert.NotNull(await LookupUpdatedAt(checkDb, resource, id));
        }

        [Theory]
        [InlineData("Categories", "Admin")]
        [InlineData("Categories", "Owner")]
        [InlineData("Colors", "Admin")]
        [InlineData("Colors", "Owner")]
        [InlineData("Sizes", "Admin")]
        [InlineData("Sizes", "Owner")]
        public async Task DeleteUnusedLookupPermanentlyRemovesOnlyTheRequestedRecord(string resource, string role)
        {
            int id = await CreateLookup(resource);
            int otherId = await CreateLookup(resource);
            if (resource == "Categories")
            {
                using var scope = _host.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.CategorieOffers.Add(new CategorieOffer { CategoryId = id, Offer = new Offer { Code = "KEEP-OFFER" } });
                await db.SaveChangesAsync();
            }

            SetRole(role);
            var response = await _client.DeleteAsync($"/api/{resource}/admin/{id}/delete");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<ApiResponse<int>>();
            Assert.True(body!.IsSuccess);
            Assert.Equal(id, body.Data);

            using var checkScope = _host.Services.CreateScope();
            var checkDb = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
            switch (resource)
            {
                case "Categories":
                    Assert.False(await checkDb.Categories.IgnoreQueryFilters().AnyAsync(c => c.Id == id));
                    Assert.False(await checkDb.CategoryTranslations.AnyAsync(t => t.CategoryId == id));
                    Assert.False(await checkDb.CategorieOffers.AnyAsync(c => c.CategoryId == id));
                    Assert.True(await checkDb.Offers.AnyAsync(o => o.Code == "KEEP-OFFER"));
                    break;
                case "Colors":
                    Assert.False(await checkDb.Colors.IgnoreQueryFilters().AnyAsync(c => c.Id == id));
                    Assert.False(await checkDb.ColorTranslations.AnyAsync(t => t.ColorId == id));
                    break;
                default:
                    Assert.False(await checkDb.Sizes.IgnoreQueryFilters().AnyAsync(s => s.Id == id));
                    break;
            }
            Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/{resource}/{otherId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync($"/api/{resource}/admin/{id}/delete")).StatusCode);
        }

        [Theory]
        [InlineData("Categories", false)]
        [InlineData("Categories", true)]
        [InlineData("Colors", false)]
        [InlineData("Colors", true)]
        [InlineData("Sizes", false)]
        [InlineData("Sizes", true)]
        public async Task DeleteReferencedLookupReturnsConflictAndPreservesOrderData(string resource, bool deletedProduct)
        {
            await SeedDetailedOrder();
            int id;
            using (var scope = _host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await db.Products.IgnoreQueryFilters().ExecuteUpdateAsync(p => p.SetProperty(x => x.IsDeleted, deletedProduct));
                var variant = await db.Variants.IgnoreQueryFilters().Include(v => v.Product).SingleAsync();
                id = resource switch { "Categories" => variant.Product.CategoryId, "Colors" => variant.ColorId, _ => variant.SizeId };
            }

            var response = await _client.DeleteAsync($"/api/{resource}/admin/{id}/delete");
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var error = (await response.Content.ReadFromJsonAsync<ApiErrorResponse>())!;
            Assert.False(error.IsSuccess);
            Assert.Equal(409, error.StatusCode);
            Assert.Equal("DELETE", error.Method);
            Assert.Equal($"/api/{resource}/admin/{id}/delete", error.Path);
            Assert.False(string.IsNullOrWhiteSpace(error.TraceId));

            using var checkScope = _host.Services.CreateScope();
            var checkDb = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(1, await checkDb.Products.IgnoreQueryFilters().CountAsync());
            Assert.Equal(1, await checkDb.Variants.CountAsync());
            Assert.Equal(1, await checkDb.OrderDetails.CountAsync());
            Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/{resource}/{id}")).StatusCode);
        }

        [Theory]
        [InlineData("Categories", "PUT")]
        [InlineData("Categories", "DELETE")]
        [InlineData("Colors", "PUT")]
        [InlineData("Colors", "DELETE")]
        [InlineData("Sizes", "PUT")]
        [InlineData("Sizes", "DELETE")]
        public async Task LookupChangesRejectAnonymousAndRegularUsersWithoutWrites(string resource, string method)
        {
            int id = await CreateLookup(resource);
            string before = await _client.GetStringAsync($"/api/{resource}/{id}");
            string path = $"/api/{resource}/admin/{id}/{(method == "PUT" ? "edit" : "delete")}";
            SetRole("User");
            Assert.Equal(HttpStatusCode.Forbidden, (await SendLookupChange(method, path, resource)).StatusCode);
            _client.DefaultRequestHeaders.Remove("X-Test-Role");
            Assert.Equal(HttpStatusCode.Unauthorized, (await SendLookupChange(method, path, resource)).StatusCode);
            Assert.Equal(before, await _client.GetStringAsync($"/api/{resource}/{id}"));
        }

        [Theory]
        [InlineData("Categories", "PUT")]
        [InlineData("Categories", "DELETE")]
        [InlineData("Colors", "PUT")]
        [InlineData("Colors", "DELETE")]
        [InlineData("Sizes", "PUT")]
        [InlineData("Sizes", "DELETE")]
        public async Task MissingLookupReturnsNotFound(string resource, string method)
        {
            string path = $"/api/{resource}/admin/999/{(method == "PUT" ? "edit" : "delete")}";
            var response = await SendLookupChange(method, path, resource);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var error = (await response.Content.ReadFromJsonAsync<ApiErrorResponse>())!;
            Assert.Equal(404, error.StatusCode);
            Assert.Equal(path, error.Path);
            Assert.Equal(method, error.Method);
        }

        [Theory]
        [InlineData("Categories")]
        [InlineData("Colors")]
        [InlineData("Sizes")]
        public async Task InvalidLookupUpdatesDoNotChangeTheSavedRecord(string resource)
        {
            int id = await CreateLookup(resource);
            string before = await _client.GetStringAsync($"/api/{resource}/{id}");
            object invalid = resource switch
            {
                "Categories" => new CategoryDetailsAdminDto { NameAr = " ", NameEn = "Changed" },
                "Colors" => new ColorDetailsAdminDto { NameAr = "Changed", NameEn = "Changed", HexCode = "#12345" },
                _ => new SizeDetailsDto { Name = new string('S', 51), SortOrder = 8 }
            };
            var response = await _client.PutAsJsonAsync($"/api/{resource}/admin/{id}/edit", invalid);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(before, await _client.GetStringAsync($"/api/{resource}/{id}"));
            Assert.Equal(HttpStatusCode.BadRequest,
                (await _client.PutAsJsonAsync($"/api/{resource}/admin/0/edit", LookupPayload(resource))).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest,
                (await _client.DeleteAsync($"/api/{resource}/admin/0/delete")).StatusCode);
        }

        [Theory]
        [InlineData("Categories")]
        [InlineData("Colors")]
        public async Task MismatchedLookupIdsAreRejectedWithoutWrites(string resource)
        {
            int id = await CreateLookup(resource);
            string before = await _client.GetStringAsync($"/api/{resource}/{id}");
            var response = await _client.PutAsJsonAsync($"/api/{resource}/admin/{id}/edit", LookupPayload(resource, updated: true, id + 1));
            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            Assert.Equal(before, await _client.GetStringAsync($"/api/{resource}/{id}"));
        }

        [Theory]
        [InlineData("Categories")]
        [InlineData("Colors")]
        public async Task LookupUpdatesRestoreMissingTranslationsAndPreserveOtherLanguages(string resource)
        {
            int id = await CreateLookup(resource);
            using (var scope = _host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Languages.Add(new Language { Id = 11, Name = "French", Code = "fr" });
                await db.SaveChangesAsync();
                if (resource == "Categories")
                {
                    await db.CategoryTranslations.Where(t => t.CategoryId == id && t.LanguageId == 9).ExecuteDeleteAsync();
                    db.CategoryTranslations.Add(new CategoryTranslation { CategoryId = id, LanguageId = 11, Name = "French name" });
                }
                else
                {
                    await db.ColorTranslations.Where(t => t.ColorId == id && t.LanguageId == 9).ExecuteDeleteAsync();
                    db.ColorTranslations.Add(new ColorTranslation { ColorId = id, LanguageId = 11, Name = "French name" });
                }
                await db.SaveChangesAsync();
            }

            var response = await _client.PutAsJsonAsync($"/api/{resource}/admin/{id}/edit", LookupPayload(resource, updated: true));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var checkScope = _host.Services.CreateScope();
            var checkDb = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var translations = resource == "Categories"
                ? await checkDb.CategoryTranslations.Where(t => t.CategoryId == id).Select(t => new { t.LanguageId, t.Name }).ToListAsync()
                : await checkDb.ColorTranslations.Where(t => t.ColorId == id).Select(t => new { t.LanguageId, t.Name }).ToListAsync();
            Assert.Equal(3, translations.Count);
            Assert.Equal("French name", translations.Single(t => t.LanguageId == 11).Name);
            Assert.Equal("Updated Arabic", translations.Single(t => t.LanguageId == 7).Name);
            Assert.Equal("Updated English", translations.Single(t => t.LanguageId == 9).Name);
        }

        [Theory]
        [InlineData("Categories")]
        [InlineData("Colors")]
        public async Task MissingLanguagePreventsPartialLookupUpdate(string resource)
        {
            int id = await CreateLookup(resource);
            string before = await _client.GetStringAsync($"/api/{resource}/{id}");
            using (var scope = _host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await db.CategoryTranslations.Where(t => t.LanguageId == 9).ExecuteDeleteAsync();
                await db.ColorTranslations.Where(t => t.LanguageId == 9).ExecuteDeleteAsync();
                await db.Languages.Where(l => l.Id == 9).ExecuteDeleteAsync();
            }

            var response = await _client.PutAsJsonAsync($"/api/{resource}/admin/{id}/edit", LookupPayload(resource, updated: true));
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(before, await _client.GetStringAsync($"/api/{resource}/{id}"));
        }

        private async Task<int> CreateLookup(string resource)
        {
            var response = await _client.PostAsJsonAsync($"/api/{resource}/admin/create", LookupPayload(resource));
            var body = await ReadJson(response);
            return body.GetProperty("data").GetProperty("id").GetInt32();
        }

        private static object LookupPayload(string resource, bool updated = false, int? id = null) => resource switch
        {
            "Categories" => new CategoryDetailsAdminDto
            {
                Id = id, NameAr = updated ? " Updated Arabic " : "Original Arabic",
                NameEn = updated ? " Updated English " : "Original English", IsActive = !updated
            },
            "Colors" => new ColorDetailsAdminDto
            {
                Id = id, NameAr = updated ? " Updated Arabic " : "Original Arabic",
                NameEn = updated ? " Updated English " : "Original English",
                HexCode = updated ? "#aabbcc" : "#112233", IsActive = !updated
            },
            _ => new SizeDetailsDto { Name = updated ? " XL " : "M", SortOrder = updated ? 8 : 1, IsActive = !updated }
        };

        private void SetRole(string role)
        {
            _client.DefaultRequestHeaders.Remove("X-Test-Role");
            _client.DefaultRequestHeaders.Add("X-Test-Role", role);
        }

        private Task<HttpResponseMessage> SendLookupChange(string method, string path, string resource) =>
            method == "PUT" ? _client.PutAsJsonAsync(path, LookupPayload(resource, updated: true)) : _client.DeleteAsync(path);

        private static Task<DateTime> LookupCreatedAt(AppDbContext db, string resource, int id) => resource switch
        {
            "Categories" => db.Categories.Where(x => x.Id == id).Select(x => x.CreatedAt).SingleAsync(),
            "Colors" => db.Colors.Where(x => x.Id == id).Select(x => x.CreatedAt).SingleAsync(),
            _ => db.Sizes.Where(x => x.Id == id).Select(x => x.CreatedAt).SingleAsync()
        };

        private static Task<DateTime?> LookupUpdatedAt(AppDbContext db, string resource, int id) => resource switch
        {
            "Categories" => db.Categories.Where(x => x.Id == id).Select(x => x.UpdatedAt).SingleAsync(),
            "Colors" => db.Colors.Where(x => x.Id == id).Select(x => x.UpdatedAt).SingleAsync(),
            _ => db.Sizes.Where(x => x.Id == id).Select(x => x.UpdatedAt).SingleAsync()
        };
    }
}
