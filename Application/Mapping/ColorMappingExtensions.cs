using Application.DTOs;
using Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Mapping
{
    public static class ColorMappingExtensions
    {
        public static ColorDto ToDto(this Color color, int languageId)
        {
            ArgumentNullException.ThrowIfNull(color);

            return new ColorDto
            {
                Id = color.Id,
                Name = color.Translations?.FirstOrDefault(t => t.LanguageId == languageId)?.Name ?? string.Empty,
                HexCode = color.HexCode,
                IsActive = color.IsActive,
            };
        }

        public static ColorDetailsDto ToDetailsDto(this Color color, int languageId)
        {
            ArgumentNullException.ThrowIfNull(color);

            return new ColorDetailsDto
            {
                Name = color.Translations?.FirstOrDefault(t => t.LanguageId == languageId)?.Name ?? string.Empty,
                HexCode = color.HexCode,
                IsActive = color.IsActive,
            };
        }

        public static List<ColorDto> ToDtoList(this IEnumerable<Color> colors, int languageId)
        {
            ArgumentNullException.ThrowIfNull(colors);

            return [.. colors.Select(c => c.ToDto(languageId))];
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
