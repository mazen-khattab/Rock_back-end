using Core.Entities;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    internal static class LookupLanguageHelper
    {
        internal static async Task<(int ArabicId, int EnglishId)?> GetLanguageIdsAsync(IServices<Language> languageService)
        {
            // Only the IDs and codes are needed; project them in one read-only query.
            var languages = await languageService.Query(l => l.Code == "ar" || l.Code == "en", tracked: false)
                .Select(l => new { l.Id, l.Code }).ToListAsync();
            var arabic = languages.FirstOrDefault(l => string.Equals(l.Code, "ar", StringComparison.OrdinalIgnoreCase));
            var english = languages.FirstOrDefault(l => string.Equals(l.Code, "en", StringComparison.OrdinalIgnoreCase));

            return arabic != null && english != null ? (arabic.Id, english.Id) : null;
        }
    }
}
