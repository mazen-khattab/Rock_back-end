using Application.DTOs;
using Core.Entities;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Text;

namespace Application.Mapping
{
    public static class ProductMappingExtensions
    {
        public static ProductsDto ToDto(this Product product, int languageId, string baseUrl)
        {
            ArgumentNullException.ThrowIfNull(product);

            // Safely select translation values for the requested language id.
            var translation = product.Translations?
                .FirstOrDefault(t => t.LanguageId == languageId);

            var categoryName = product.Category?.Translations?
                .FirstOrDefault(ct => ct.LanguageId == languageId)?.Name
                ?? string.Empty;

            return new ProductsDto()
            {
                Id = product.Id,
                Category = categoryName,
                Name = translation?.Name ?? string.Empty,
                Price = product.Price,
                OriginalPrice = product.OriginalPrice,
                Slug = translation?.Slug ?? string.Empty,
                IsActive = !product.IsDeleted,
                Variants = product.Variants?.ToDtoList(languageId, baseUrl) ?? new List<VariantsDto>()
            };
        }

        public static List<ProductsDto> ToDtoList(this IEnumerable<Product> products, int languageId, string baseUrl)
        {
            ArgumentNullException.ThrowIfNull(products);

            return products.Select(p => p.ToDto(languageId, baseUrl)).ToList();
        }

        public static ProductDetailsDto ToDetailsDto(this Product product, int languageId, string baseUrl)
        {
            ArgumentNullException.ThrowIfNull(product);

            // Safely select translation values for the requested language id.
            var translation = product.Translations?
                .FirstOrDefault(t => t.LanguageId == languageId);

            var categoryName = product.Category?.Translations?
                .FirstOrDefault(ct => ct.LanguageId == languageId)?.Name
                ?? string.Empty;

            return new ProductDetailsDto()
            {
                Id = product.Id,
                Category = categoryName,
                Name = translation?.Name ?? string.Empty,
                Description = translation?.Description ?? string.Empty,
                Price = product.Price,
                OriginalPrice = product.OriginalPrice,
                Slug = translation?.Slug ?? string.Empty,
                MetaTitle = translation?.MetaTitle ?? string.Empty,
                MetaDescription = translation?.MetaDescription ?? string.Empty,
                IsActive = !product.IsDeleted,
                Variants = product.Variants?.ToDetailsDtoList(languageId, baseUrl) ?? new List<VariantDetailsDto>()
            };
        }

        public static ProductsAdminDto ToAdminDto(this Product product, int languageId, string baseURL)
        {
            ArgumentNullException.ThrowIfNull(product);

            // Safely select translation values for the requested language id.
            var translation = product.Translations?
                .FirstOrDefault(t => t.LanguageId == languageId);

            var categoryName = product.Category?.Translations?
                .FirstOrDefault(ct => ct.LanguageId == languageId)?.Name
                ?? string.Empty;

            // Extract all unique sizes from all variants
            var availableSizes = product.Variants?
                .Where(v => v.Size != null)
                .Select(v => v.Size.Name)
                .Distinct()
                .OrderBy(s => s)
                .ToList() ?? new List<string>();

            // Calculate total quantity and reserved from all variants
            var totalQuantity = product.Variants?.Sum(v => v.Quantity) ?? 0;
            var totalReserved = product.Variants?.Sum(v => v.Reserved) ?? 0;
            var availableQuantity = totalQuantity - totalReserved;

            // Get the first image from the first variant
            var imageUrl = product.Variants?
                .FirstOrDefault()?.Images?
                .FirstOrDefault()?.MediaAsset?.FilePath
                ?? string.Empty;

            return new ProductsAdminDto()
            {
                Id = product.Id,
                Name = translation?.Name ?? string.Empty,
                Category = categoryName,
                Quantity = totalQuantity,
                Reserved = totalReserved,
                Available = availableQuantity,
                Price = product.Price,
                IsActive = product.IsActive,
                Sizes = availableSizes,
                ImageUrl = $"{baseURL}/{imageUrl}".TrimEnd('/')
            };
        }

        public static List<ProductsAdminDto> ToDtoAdminList(this IEnumerable<Product> products, int languageId, string baseURL)
        {
            ArgumentNullException.ThrowIfNull(products);

            return products.Select(p => p.ToAdminDto(languageId, baseURL)).ToList();
        }

        public static ProductDetailsAdminDto ToAdminDetailsDto(this Product product, string baseURL)
        {
            ArgumentNullException.ThrowIfNull(product);

            var enTranslation = product.Translations?
                .FirstOrDefault(t => (t.Language?.Code ?? string.Empty) == "en");

            var arTranslation = product.Translations?
                .FirstOrDefault(t => (t.Language?.Code ?? string.Empty) == "ar");

            return new ProductDetailsAdminDto()
            {
                Id = product.Id,
                CategoryId = product.CategoryId,
                NameAr = arTranslation?.Name ?? string.Empty,
                NameEn = enTranslation?.Name ?? string.Empty,
                DescriptionAr = arTranslation?.Description ?? string.Empty,
                DescriptionEn = enTranslation?.Description ?? string.Empty,
                Price = product.Price,
                SlugAr = arTranslation?.Slug ?? string.Empty,
                SlugEn = enTranslation?.Slug ?? string.Empty,
                MetaTitleAr = arTranslation?.MetaTitle ?? string.Empty,
                MetaTitleEn = enTranslation?.MetaTitle ?? string.Empty,
                MetaDescriptionAr = arTranslation?.MetaDescription ?? string.Empty,
                MetaDescriptionEn = enTranslation?.MetaDescription ?? string.Empty,
                IsActive = product.IsActive,
                Variants = product.Variants.ToAdminDetailsDtoList(baseURL) ?? new List<VariantsDetailsAdminDto>()
            };
        }

        public static Product FromDetialsAdminDto(this Product product, ProductDetailsAdminDto dto)
        {
            ArgumentNullException.ThrowIfNull(product);
            ArgumentNullException.ThrowIfNull(dto);

            product.CategoryId = dto.CategoryId;
            product.Price = dto.Price;
            product.IsActive = dto.IsActive;
            product.OriginalPrice = dto.Price;

            return product;
        }
    }
}
