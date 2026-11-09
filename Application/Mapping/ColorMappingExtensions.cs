using Application.DTOs;
using Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Mapping
{
    public static class ColorMappingExtensions
    {
        public static void FromAdminDetailsDto(this Color color, ColorDetailsAdminDto dto, int arabicLanguageId, int englishLanguageId)
        {
            color.HexCode = dto.HexCode.ToUpperInvariant();
            color.IsActive = dto.IsActive;
            SetTranslation(color, arabicLanguageId, dto.NameAr);
            SetTranslation(color, englishLanguageId, dto.NameEn);
        }

        private static void SetTranslation(Color color, int languageId, string name)
        {
            var translation = color.Translations.FirstOrDefault(t => t.LanguageId == languageId);
            if (translation == null)
            {
                translation = new ColorTranslation { LanguageId = languageId };
                color.Translations.Add(translation);
            }

            translation.Name = name.Trim();
        }

        public static ColorDetailsAdminDto ToAdminDetailsDto(this Color color, int arabicLanguageId, int englishLanguageId)
        {
            ArgumentNullException.ThrowIfNull(color);

            return new ColorDetailsAdminDto
            {
                Id = color.Id,
                NameAr = color.Translations.FirstOrDefault(t => t.LanguageId == arabicLanguageId)?.Name ?? string.Empty,
                NameEn = color.Translations.FirstOrDefault(t => t.LanguageId == englishLanguageId)?.Name ?? string.Empty,
                HexCode = color.HexCode,
                IsActive = color.IsActive
            };
        }

        public static IQueryable<ColorDto> ProjectToDto(this IQueryable<Color> colors)
        {
            // Only select the fields returned by the API, without loading translation entities.
            return colors.Select(color => new ColorDto
            {
                Id = color.Id,
                NameAr = color.Translations.Where(t => t.Language.Code == "ar")
                    .Select(t => t.Name).FirstOrDefault() ?? string.Empty,
                NameEn = color.Translations.Where(t => t.Language.Code == "en")
                    .Select(t => t.Name).FirstOrDefault() ?? string.Empty,
                HexCode = color.HexCode,
                IsActive = color.IsActive
            });
        }

        public static IQueryable<ColorDetailsDto> ProjectToDetailsDto(this IQueryable<Color> colors)
        {
            return colors.ProjectToDto().Select(color => new ColorDetailsDto
            {
                NameAr = color.NameAr,
                NameEn = color.NameEn,
                HexCode = color.HexCode,
                IsActive = color.IsActive
            });
        }

        public static ColorLookupDto ToLookupDto(this Color lookup, int languageId)
        {
            ArgumentNullException.ThrowIfNull(lookup);

            return new ColorLookupDto
            {
                Id = lookup.Id,
                Name = lookup.Translations.FirstOrDefault(t => t.LanguageId == languageId)?.Name ?? string.Empty,
                HexCode = lookup.HexCode
            };
        }

        public static List<ColorLookupDto> ToListLookupDto(this IEnumerable<Color> lookup, int languageId)
        {
            ArgumentNullException.ThrowIfNull(lookup);

            return [.. lookup.Select(c => c.ToLookupDto(languageId))];
        }
    }
}
