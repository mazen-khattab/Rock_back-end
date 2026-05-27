using Application.DTOs;
using Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Mapping
{
    public static class ColorMappingExtensions
    {
        public static colorDto ToDto(this Color color, int languageId)
        {
            if (color is null)
            {
                throw new ArgumentNullException(nameof(Color));
            }

            return new colorDto
            {
                Id = color.Id,
                Name = color.Translations?.FirstOrDefault(t => t.LanguageId == languageId)?.Name ?? string.Empty,
                HexCode = color.HexCode,
                IsActive = color.IsActive,
            };
        }

        public static colorDetailsDto ToDetailsDto(this Color color, int languageId)
        {
            if (color is null)
            {
                throw new ArgumentNullException(nameof(Color));
            }

            return new colorDetailsDto
            {
                Name = color.Translations?.FirstOrDefault(t => t.LanguageId == languageId)?.Name ?? string.Empty,
                HexCode = color.HexCode,
                IsActive = color.IsActive,
            };
        }

        public static List<colorDto> ToDtoList(this IEnumerable<Color> colors, int languageId)
        {
            if (colors is null)
            {
                throw new ArgumentNullException(nameof(Color));
            }

            return colors.Select(c => c.ToDto(languageId)).ToList();
        }

        public static ColorLookupDto ToLookupDto(this Color lookup, int languageId)
        {
            if (lookup == null)
            {
                throw new ArgumentNullException(nameof(Color));
            }

            return new ColorLookupDto
            {
                Id = lookup.Id,
                Name = lookup.Translations.FirstOrDefault(t => t.LanguageId == languageId)?.Name ?? string.Empty,
                HexCode = lookup.HexCode
            };
        }

        public static List<ColorLookupDto> ToListLookupDto(this IEnumerable<Color> lookup, int languageId)
        {
            if (lookup == null)
            {
                throw new ArgumentNullException(nameof(Color));
            }

            return lookup.Select(c => c.ToLookupDto(languageId)).ToList();
        }
    }
}
