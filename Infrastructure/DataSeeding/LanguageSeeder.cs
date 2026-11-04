using Core.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.DataSeeding
{
    public static class LanguageSeeder
    {
        public static async Task SeedAsync(AppDbContext context, ILogger logger)
        {
            logger.LogInformation("Seeding supported languages");

            var existingCodes = await context.Languages.AsNoTracking()
                .Select(l => l.Code).ToListAsync();
            var languages = new List<Language>
            {
                new() { Name = "Arabic", Code = "ar", CreatedAt = DateTime.Now },
                new() { Name = "English", Code = "en", CreatedAt = DateTime.Now }
            };

            // Check each code so repeated startups also handle partially seeded databases.
            var missingLanguages = languages.Where(l =>
                !existingCodes.Contains(l.Code, StringComparer.OrdinalIgnoreCase)).ToList();

            if (missingLanguages.Count == 0)
            {
                logger.LogDebug("Arabic and English languages already exist; skipping language seeding");
                return;
            }

            // Save only new records; existing names, IDs and creation dates remain unchanged.
            context.Languages.AddRange(missingLanguages);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded {Count} languages with codes: {LanguageCodes}",
                missingLanguages.Count, string.Join(", ", missingLanguages.Select(l => l.Code)));
        }
    }
}
