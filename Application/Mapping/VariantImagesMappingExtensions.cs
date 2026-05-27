using Application.DTOs;
using Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Mapping
{
    public static class VariantImagesMappingExtensions
    {
        public static VariantImagesDto ToDto(this VariantImage variantImage)
        {
            if (variantImage == null)
            {
                throw new ArgumentNullException(nameof(variantImage));
            }

            var media = variantImage.MediaAsset;

            var dto = new VariantImagesDto()
            {
                ImageUrl = media?.FilePath ?? string.Empty,
                AltText = media?.AltText ?? string.Empty,
            };

            return dto;
        }

        public static List<VariantImagesDto> ToDtoList(this IEnumerable<VariantImage> variantImages)
        {
            if (variantImages == null)
            {
                throw new ArgumentNullException(nameof(variantImages));
            }

            return variantImages.Select(ToDto).ToList();
        }
    }
}
