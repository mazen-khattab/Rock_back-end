using Application.DTOs;
using Core.Entities;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Mapping
{
    public static class ProductMappingExtensions
    {
        public static AllProductsDto ToDto(this Product product, int languageId)
        {
            if (product == null)
            {
                throw new ArgumentNullException(nameof(product));
            }

            // Safely select translation values for the requested language id.
            var translation = product.Translations?
                .FirstOrDefault(t => t.LanguageId == languageId);

            var categoryName = product.Category?.Translations?
                .FirstOrDefault(ct => ct.LanguageId == languageId)?.Name
                ?? string.Empty;

            return new AllProductsDto()
            {
                Id = product.Id,
                Category = categoryName,
                Name = translation?.Name ?? string.Empty,
                Price = product.Price,
                OriginalPrice = product.OriginalPrice,
                Slug = translation?.Slug ?? string.Empty,
                IsActive = !product.IsDeleted,
                Variants = product.Variants?.ToDtoList(languageId) ?? new List<AllProductsVariantsDto>()
            };

        }

        public static ProductDetailsDto ToDetailsDto(this Product product, int languageId)
        {
            if (product == null)
            {
                throw new ArgumentNullException(nameof(product));
            }

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
                Variants = product.Variants?.ToDetailsDtoList(languageId) ?? new List<ProductDetailsVariantDto>()
            };
        }

        public static AllProductsAdminDto ToAdminDto(this Product product, int languageId, string baseURL)
        {
            if (product == null)
            {
                throw new ArgumentNullException(nameof(product));
            }

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

            return new AllProductsAdminDto()
            {
                Id = product.Id,
                Name = translation?.Name ?? string.Empty,
                Category = categoryName,
                Quantity = totalQuantity,
                Reserved = totalReserved,
                Available = availableQuantity,
                Price = product.Price,
                IsActive = !product.IsDeleted,
                Sizes = availableSizes,
                ImageUrl = $"{baseURL}/{imageUrl}".TrimEnd('/')
            };
        }

        public static List<AllProductsDto> ToDtoList(this IEnumerable<Product> products, int languageId)
        {
            if (products == null)
            {
                throw new ArgumentNullException(nameof(products));
            }

            return products.Select(p => p.ToDto(languageId)).ToList();
        }

        public static List<AllProductsAdminDto> ToDtoAdminList(this IEnumerable<Product> products, int languageId, string baseURL)
        {
            if (products == null)
            {
                throw new ArgumentNullException(nameof(products));
            }

            return products.Select(p => p.ToAdminDto(languageId, baseURL)).ToList();
        }

    }
}
