using Application.DTOs;
using Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Mapping
{
    public static class VariantMappingExtensions
    {
        public static AllProductsVariantsDto ToDto(this Variant variant, int languageId)
        {
            if (variant == null)
            {
                throw new ArgumentNullException(nameof(variant));
            }

            // For color, translations exist on ColorTranslation with LanguageId.
            var colorName = variant.Color?.Translations?
                .FirstOrDefault(ct => ct.LanguageId == languageId)?.Name
                ?? string.Empty;

            // Size has a single Name property (no translations) in this model.
            var sizeName = variant.Size?.Name ?? string.Empty;

            var dto = new AllProductsVariantsDto()
            {
                Id = variant.Id,
                ProductId = variant.ProductId,
                ColorName = colorName,
                HexCode = variant.Color?.HexCode ?? string.Empty,
                SizeName = sizeName,
                Quantity = variant.Quantity,
                Reserved = variant.Reserved,
                ImagesDtos = variant.Images?.FirstOrDefault()?.ToDto() ?? new VariantImagesDto()
            };

            return dto;
        }

        public static ProductDetailsVariantDto ToDetailsDto(this Variant variant, int languageId)
        {
            if (variant == null)
            {
                throw new ArgumentNullException(nameof(variant));
            }

            // For color, translations exist on ColorTranslation with LanguageId.
            var colorName = variant.Color?.Translations?
                .FirstOrDefault(ct => ct.LanguageId == languageId)?.Name
                ?? string.Empty;

            // Size has a single Name property (no translations) in this model.
            var sizeName = variant.Size?.Name ?? string.Empty;

            var dto = new ProductDetailsVariantDto()
            {
                Id = variant.Id,
                ProductId = variant.ProductId,
                ColorName = colorName,
                HexCode = variant.Color?.HexCode ?? string.Empty,
                SizeName = sizeName,
                Quantity = variant.Quantity,
                Reserved = variant.Reserved,
                ImagesDtos = variant.Images.ToDtoList() ?? new List<VariantImagesDto>()
            };

            return dto;
        }

        public static List<AllProductsVariantsDto> ToDtoList(this IEnumerable<Variant> variants, int languageId)
        {
            if (variants == null)
            {
                throw new ArgumentNullException(nameof(variants));
            }

            return variants.Select(v => v.ToDto(languageId)).ToList();
        }

        public static List<ProductDetailsVariantDto> ToDetailsDtoList(this IEnumerable<Variant> variants, int languageId)
        {
            if (variants == null)
            {
                throw new ArgumentNullException(nameof(variants));
            }

            return variants.Select(v => v.ToDetailsDto(languageId)).ToList();
        }

    }
}
