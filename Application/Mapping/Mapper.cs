using Application.DTOs;
using Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Application.Mapping
{
    public static class Mapper
    {
        #region Map from Entity List to Dto List

        public static List<ProductsDto> MapToDtoList(IEnumerable<Product> products, int languageId)
        {
            if (products == null) throw new ArgumentNullException(nameof(products));

            var productDtos = new List<ProductsDto>();
            foreach (var product in products)
            {
                productDtos.Add(MapToDto(product, languageId));
            }
            return productDtos;
        }

        public static List<VariantDto> MapToDtoList(IEnumerable<Variant> variants, int languageId)
        {
            if (variants == null) throw new ArgumentNullException(nameof(variants));

            var variantDtos = new List<VariantDto>();
            foreach (var variant in variants)
            {
                variantDtos.Add(MapToDto(variant, languageId));
            }
            return variantDtos;
        }

        public static List<VariantImagesDto> MapToDtoList(IEnumerable<VariantImage> variantImages)
        {
            if (variantImages == null) throw new ArgumentNullException(nameof(variantImages));

            var variantImageDtos = new List<VariantImagesDto>();
            foreach (var variantImage in variantImages)
            {
                variantImageDtos.Add(MapToDto(variantImage));
            }
            return variantImageDtos;
        }
        #endregion


        #region Map from Entity to Dto

        public static ProductsDto MapToDto(Product product, int languageId)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));

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
                Description = translation?.Description ?? string.Empty,
                Price = product.Price,
                OriginalPrice = product.OriginalPrice,
                Slug = translation?.Slug ?? string.Empty,
                MetaTitle = translation?.MetaTitle ?? string.Empty,
                MetaDescription = translation?.MetaDescription ?? string.Empty,
                Variants = MapToDtoList(product.Variants ?? Enumerable.Empty<Variant>(), languageId)
            };
        }

        public static VariantDto MapToDto(Variant variant, int languageId)
        {
            if (variant == null) throw new ArgumentNullException(nameof(variant));

            // For color, translations exist on ColorTranslation with LanguageId.
            var colorName = variant.Color?.Translations?
                .FirstOrDefault(ct => ct.LanguageId == languageId)?.Name
                ?? string.Empty;

            // Size has a single Name property (no translations) in this model.
            var sizeName = variant.Size?.Name ?? string.Empty;

            return new VariantDto()
            {
                Id = variant.Id,
                ProductId = variant.ProductId,
                ColorName = colorName,
                HexCode = variant.Color?.HexCode ?? string.Empty, 
                SizeName = sizeName,
                Quantity = variant.Quantity,
                Reserved = variant.Reserved,
                ImagesDtos = MapToDtoList(variant.Images ?? Enumerable.Empty<VariantImage>())
                //ColorId = variant.ColorId,
                //SizeId = variant.SizeId,
            };
        }

        public static VariantImagesDto MapToDto(VariantImage variantImage)
        {
            if (variantImage == null) throw new ArgumentNullException(nameof(variantImage));

            // Be defensive about MediaAsset navigation property.
            var media = variantImage.MediaAsset;

            return new VariantImagesDto()
            {
                ImageUrl = media?.FilePath ?? string.Empty,
                AltText = media?.AltText ?? string.Empty,
                //VariantImageId = variantImage.Id,
                //VariantId = variantImage.VariantId,
                //MediaAssetId = variantImage.MediaAssetId,
                //SortOrder = variantImage.SortOrder,
                //fileName = media?.FileName ?? string.Empty,
                //MediaType = media?.MediaType ?? string.Empty
            };
        }
        #endregion
    }
}
