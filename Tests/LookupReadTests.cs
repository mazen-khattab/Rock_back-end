using System.Text.Json;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests
{
    public partial class AdminEndpointsTests
    {
        [Theory]
        [InlineData("Categories", "ar")]
        [InlineData("Categories", "en")]
        [InlineData("Colors", "ar")]
        [InlineData("Colors", "en")]
        public async Task LookupListAndDetailsReturnBothNamesRegardlessOfRequestLanguage(string resource, string language)
        {
            int id = await CreateLookup(resource);
            _client.DefaultRequestHeaders.Add("Accept-Language", language);
            // These read endpoints are public, just as before the response change.
            _client.DefaultRequestHeaders.Remove("X-Test-Role");

            var details = await ReadJson(await _client.GetAsync($"/api/{resource}/{id}"));
            AssertBilingualNames(details.GetProperty("data"), "Original Arabic", "Original English");
            Assert.True(details.GetProperty("data").GetProperty("isActive").GetBoolean());
            if (resource == "Colors")
                Assert.Equal("#112233", details.GetProperty("data").GetProperty("hexCode").GetString());

            var list = await ReadJson(await _client.GetAsync($"/api/{resource}"));
            var item = Assert.Single(list.GetProperty("items").EnumerateArray());
            Assert.Equal(id, item.GetProperty("id").GetInt32());
            AssertBilingualNames(item, "Original Arabic", "Original English");
            Assert.Equal(1, list.GetProperty("totalCount").GetInt32());
            Assert.Equal(1, list.GetProperty("pageNumber").GetInt32());
            Assert.Equal(12, list.GetProperty("pageSize").GetInt32());
            if (resource == "Colors")
                Assert.Equal("#112233", item.GetProperty("hexCode").GetString());
        }

        [Theory]
        [InlineData("Categories", "ar")]
        [InlineData("Categories", "en")]
        [InlineData("Colors", "ar")]
        [InlineData("Colors", "en")]
        public async Task MissingTranslationReturnsEmptyNameWithoutHidingTheRecord(string resource, string missingLanguage)
        {
            int id = await CreateLookup(resource);
            using (var scope = _host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                int languageId = missingLanguage == "ar" ? 7 : 9;
                if (resource == "Categories")
                    await db.CategoryTranslations.Where(t => t.CategoryId == id && t.LanguageId == languageId).ExecuteDeleteAsync();
                else
                    await db.ColorTranslations.Where(t => t.ColorId == id && t.LanguageId == languageId).ExecuteDeleteAsync();
            }

            string arabic = missingLanguage == "ar" ? string.Empty : "Original Arabic";
            string english = missingLanguage == "en" ? string.Empty : "Original English";
            var details = await ReadJson(await _client.GetAsync($"/api/{resource}/{id}"));
            AssertBilingualNames(details.GetProperty("data"), arabic, english);
            var list = await ReadJson(await _client.GetAsync($"/api/{resource}"));
            AssertBilingualNames(Assert.Single(list.GetProperty("items").EnumerateArray()), arabic, english);
            Assert.Equal(1, list.GetProperty("totalCount").GetInt32());
        }

        [Theory]
        [InlineData("Categories", "ar")]
        [InlineData("Categories", "en")]
        [InlineData("Colors", "ar")]
        [InlineData("Colors", "en")]
        public async Task DropdownLookupsKeepTheirLocalizedName(string resource, string language)
        {
            int id = await CreateLookup(resource);
            _client.DefaultRequestHeaders.Add("Accept-Language", language);
            var lookup = await ReadJson(await _client.GetAsync($"/api/{resource}/lookup"));
            var item = Assert.Single(lookup.GetProperty("data").EnumerateArray());
            Assert.Equal(id, item.GetProperty("id").GetInt32());
            Assert.Equal(language == "ar" ? "Original Arabic" : "Original English", item.GetProperty("name").GetString());
            Assert.False(item.TryGetProperty("nameAr", out _));
            Assert.False(item.TryGetProperty("nameEn", out _));
        }

        [Theory]
        [InlineData("Categories")]
        [InlineData("Colors")]
        public async Task BilingualListMatchesItsDeclaredPageSize(string resource)
        {
            for (int i = 0; i < 13; i++)
                await CreateLookup(resource);

            var list = await ReadJson(await _client.GetAsync($"/api/{resource}"));
            Assert.Equal(13, list.GetProperty("totalCount").GetInt32());
            Assert.Equal(12, list.GetProperty("items").GetArrayLength());
            Assert.Equal(2, list.GetProperty("totalPages").GetInt32());
            Assert.All(list.GetProperty("items").EnumerateArray(), item =>
                AssertBilingualNames(item, "Original Arabic", "Original English"));
        }

        private static void AssertBilingualNames(JsonElement item, string arabic, string english)
        {
            Assert.Equal(arabic, item.GetProperty("nameAr").GetString());
            Assert.Equal(english, item.GetProperty("nameEn").GetString());
            Assert.False(item.TryGetProperty("name", out _));
        }
    }
}
