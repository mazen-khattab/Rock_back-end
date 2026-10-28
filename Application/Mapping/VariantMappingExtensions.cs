using Application.DTOs;
using Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Mapping
{
    public static class VariantMappingExtensions
    {
        public static VariantsDto ToDto(this Variant variant, int languageId, string baseUrl)
        {
            ArgumentNullException.ThrowIfNull(variant);

            // For color, translations exist on ColorTranslation with LanguageId.
            var colorName = variant.Color?.Translations?
                .FirstOrDefault(ct => ct.LanguageId == languageId)?.Name
                ?? string.Empty;

            // Size has a single Name property (no translations) in this model.
            var sizeName = variant.Size?.Name ?? string.Empty;

            var dto = new VariantsDto()
            {
                Id = variant.Id,
                ProductId = variant.ProductId,
                ColorName = colorName,
                HexCode = variant.Color?.HexCode ?? string.Empty,
                SizeName = sizeName,
                Quantity = variant.Quantity,
                Reserved = variant.Reserved,
                ImagesDtos = variant.Images?.FirstOrDefault()?.ToDto(baseUrl) ?? new ImagesDto()
            };

            return dto;
        }

        public static List<VariantsDto> ToDtoList(this IEnumerable<Variant> variants, int languageId, string baseUrl)
        {
            ArgumentNullException.ThrowIfNull(variants);

            return variants.Select(v => v.ToDto(languageId, baseUrl)).ToList();
        }

        public static VariantDetailsDto ToDetailsDto(this Variant variant, int languageId, string baseUrl)
        {
            ArgumentNullException.ThrowIfNull(variant);

            // For color, translations exist on ColorTranslation with LanguageId.
            var colorName = variant.Color?.Translations?
                .FirstOrDefault(ct => ct.LanguageId == languageId)?.Name
                ?? string.Empty;

            // Size has a single Name property (no translations) in this model.
            var sizeName = variant.Size?.Name ?? string.Empty;

            var dto = new VariantDetailsDto()
            {
                Id = variant.Id,
                ProductId = variant.ProductId,
                ColorName = colorName,
                HexCode = variant.Color?.HexCode ?? string.Empty,
                SizeName = sizeName,
                Quantity = variant.Quantity,
                Reserved = variant.Reserved,
                ImagesDtos = variant.Images.ToDtoList(baseUrl) ?? new List<ImagesDto>()
            };

            return dto;
        }

        public static List<VariantDetailsDto> ToDetailsDtoList(this IEnumerable<Variant> variants, int languageId, string baseUrl)
        {
            ArgumentNullException.ThrowIfNull(variants);

            return variants.Select(v => v.ToDetailsDto(languageId, baseUrl)).ToList();
        }

        public static VariantsDetailsAdminDto ToAdminDetailsDto(this Variant variant, string baseURL)
        {
            ArgumentNullException.ThrowIfNull(variant);

            var dto = new VariantsDetailsAdminDto()
            {
                Id = variant.Id,
                ColorId = variant.ColorId,
                SizeId = variant.SizeId,
                Quantity = variant.Quantity,
                Images = variant.Images.ToAdminDtoList(baseURL) ?? new List<ImagesAdminDto>()
            };
            return dto;
        }

        public static List<VariantsDetailsAdminDto> ToAdminDetailsDtoList(this IEnumerable<Variant> variants, string baseURL)
        {
            ArgumentNullException.ThrowIfNull(variants);

            return variants.Select(v => v.ToAdminDetailsDto(baseURL)).ToList();
        }
    }
}
