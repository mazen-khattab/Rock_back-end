using Application.DTOs;
using Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Mapping
{
    public static class CategoryMappingExtensions
    {
        public static void FromAdminDetailsDto(this Category category, CategoryDetailsAdminDto dto, int arabicLanguageId, int englishLanguageId)
        {
            category.IsActive = dto.IsActive;
            SetTranslation(category, arabicLanguageId, dto.NameAr);
            SetTranslation(category, englishLanguageId, dto.NameEn);
        }

        private static void SetTranslation(Category category, int languageId, string name)
        {
            var translation = category.Translations.FirstOrDefault(t => t.LanguageId == languageId);
            if (translation == null)
            {
                translation = new CategoryTranslation { LanguageId = languageId };
                category.Translations.Add(translation);
            }

            translation.Name = name.Trim();
        }

        public static CategoryDetailsAdminDto ToAdminDetailsDto(this Category category, int arabicLanguageId, int englishLanguageId)
        {
            ArgumentNullException.ThrowIfNull(category);

            return new CategoryDetailsAdminDto
            {
                Id = category.Id,
                NameAr = category.Translations.FirstOrDefault(t => t.LanguageId == arabicLanguageId)?.Name ?? string.Empty,
                NameEn = category.Translations.FirstOrDefault(t => t.LanguageId == englishLanguageId)?.Name ?? string.Empty,
                IsActive = category.IsActive
            };
        }

        public static IQueryable<CategoryDto> ProjectToDto(this IQueryable<Category> categories)
        {
            // Project both names in SQL using language codes rather than fixed seed IDs.
            return categories.Select(category => new CategoryDto
            {
                Id = category.Id,
                IsActive = category.IsActive,
                NameAr = category.Translations.Where(t => t.Language.Code == "ar")
                    .Select(t => t.Name).FirstOrDefault() ?? string.Empty,
                NameEn = category.Translations.Where(t => t.Language.Code == "en")
                    .Select(t => t.Name).FirstOrDefault() ?? string.Empty
            });
        }

        public static IQueryable<CategoryDetailsDto> ProjectToDetailsDto(this IQueryable<Category> categories)
        {
            return categories.ProjectToDto().Select(category => new CategoryDetailsDto
            {
                IsActive = category.IsActive,
                NameAr = category.NameAr,
                NameEn = category.NameEn
            });
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
