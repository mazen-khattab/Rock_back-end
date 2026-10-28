using Application.DTOs;
using Core.Entities;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Text;

namespace Application.Mapping
{
    public static class ImagesMappingExtensions
    {
        public static ImagesDto ToDto(this VariantImage variantImage, string baseUrl)
        {
            ArgumentNullException.ThrowIfNull(variantImage);

            var media = variantImage.MediaAsset;

            var dto = new ImagesDto()
            {
                ImageUrl = $"{baseUrl}/{media?.FilePath ?? string.Empty}".TrimEnd('/'),
                AltText = media?.AltText ?? string.Empty,
            };

            return dto;
        }

        public static List<ImagesDto> ToDtoList(this IEnumerable<VariantImage> variantImages, string baseUrl)
        {
            ArgumentNullException.ThrowIfNull(variantImages);

            return variantImages.Select(vi => vi.ToDto(baseUrl)).ToList();
        }

        public static ImagesAdminDto ToAdminDto(this VariantImage variantImage, string baseUrl)
        {
            ArgumentNullException.ThrowIfNull(variantImage);

            var media = variantImage.MediaAsset;

            var dto = new ImagesAdminDto()
            {
                Id = variantImage.Id,
                ImageUrl = $"{baseUrl}/{media?.FilePath ?? string.Empty}".TrimEnd('/'),
                sortOrder = variantImage.SortOrder
            };
            return dto;
        }

        public static List<ImagesAdminDto> ToAdminDtoList(this IEnumerable<VariantImage> variantImages, string baseUrl)
        {
            ArgumentNullException.ThrowIfNull(variantImages);

            return variantImages.Select(vi => ToAdminDto(vi, baseUrl)).ToList();
        }
    }
}
