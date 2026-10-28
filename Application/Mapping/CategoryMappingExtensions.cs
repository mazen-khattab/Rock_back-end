using Application.DTOs;
using Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Mapping
{
    public static class CategoryMappingExtensions
    {
        public static CategoryDto ToDto(this Category category, int languageId)
        {
            ArgumentNullException.ThrowIfNull(category);

            string trasnlationName = category.Translations?.FirstOrDefault(t => t.LanguageId == languageId)?.Name ?? string.Empty;

            return new CategoryDto()
            {
                Id = category.Id,
                IsActive = category.IsActive,
                Name = trasnlationName,
            };

        }

        public static CategoryDetailsDto ToDetailsDto(this Category category, int languageId)
        {
            ArgumentNullException.ThrowIfNull(category);

            string trasnlationName = category.Translations?.FirstOrDefault(t => t.LanguageId == languageId)?.Name ?? string.Empty;

            return new CategoryDetailsDto()
            {
                IsActive = category.IsActive,
                Name = trasnlationName,
            };

        }

        public static List<CategoryDto> ToDtoList(this IEnumerable<Category> categories, int languageId)
        {
            ArgumentNullException.ThrowIfNull(categories);

            return categories.Select(c => c.ToDto(languageId)).ToList();
        }

        public static CategoryLookupDto ToLookupDto(this Category lookup, int languageId)
        {
            ArgumentNullException.ThrowIfNull(lookup);

            return new CategoryLookupDto
            {
                Id = lookup.Id,
                Name = lookup.Translations?.FirstOrDefault(t => t.LanguageId == languageId)?.Name ?? string.Empty
            };
        }

        public static List<CategoryLookupDto> ToListLookupDto(this IEnumerable<Category> lookup, int languageId)
        {
            ArgumentNullException.ThrowIfNull(lookup);

            return lookup.Select(c => c.ToLookupDto(languageId)).ToList();
        }
    }
}
