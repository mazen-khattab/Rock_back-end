using Application.DTOs;
using Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Mapping
{
    public static class CategoryMappingExtensions
    {
        public static categoryDto ToDto(this Category category, int languageId)
        {
            if (category == null)
            {
                throw new ArgumentNullException(nameof(Category));
            }

            string trasnlationName = category.Translations?.FirstOrDefault(t => t.LanguageId == languageId)?.Name ?? string.Empty;

            return new categoryDto()
            {
                Id = category.Id,
                IsActive = category.IsActive,
                Name = trasnlationName,
            };

        }

        public static categoryDetailsDto ToDetailsDto(this Category category, int languageId)
        {
            if (category == null)
            {
                throw new ArgumentNullException(nameof(Category));
            }

            string trasnlationName = category.Translations?.FirstOrDefault(t => t.LanguageId == languageId)?.Name ?? string.Empty;

            return new categoryDetailsDto()
            {
                IsActive = category.IsActive,
                Name = trasnlationName,
            };

        }

        public static List<categoryDto> ToDtoList(this IEnumerable<Category> categories, int languageId)
        {
            if (categories == null)
            {
                throw new ArgumentNullException(nameof(Category));
            }

            return categories.Select(c => c.ToDto(languageId)).ToList();
        }

        public static CategoryLookupDto ToLookupDto(this Category lookup, int languageId)
        {
            if (lookup == null)
            {
                throw new ArgumentNullException(nameof(Category));
            }

            return new CategoryLookupDto
            {
                Id = lookup.Id,
                Name = lookup.Translations?.FirstOrDefault(t => t.LanguageId == languageId)?.Name ?? string.Empty
            };
        }

        public static List<CategoryLookupDto> ToListLookupDto(this IEnumerable<Category> lookup, int languageId)
        {
            if (lookup == null)
            {
                throw new ArgumentNullException(nameof(Category));
            }

            return lookup.Select(c => c.ToLookupDto(languageId)).ToList();
        }
    }
}
