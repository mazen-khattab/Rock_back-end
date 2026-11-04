using Application.DTOs;
using Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Mapping
{
    public static class OrderDetailsMappingExtensions
    {
        public static OrderItemAdminDto ToAdminDetailsDto(this OrderDetail orderDetail, int languageId, string baseUrl)
        {
            ArgumentNullException.ThrowIfNull(orderDetail);

            var variant = orderDetail.Variant;
            var productTranslation = variant?.Product?.Translations.FirstOrDefault(t => t.LanguageId == languageId);
            var colorTranslation = variant?.Color?.Translations.FirstOrDefault(t => t.LanguageId == languageId);
            var imagePath = variant?.Images.OrderBy(i => i.SortOrder).ThenBy(i => i.Id)
                .FirstOrDefault()?.MediaAsset?.FilePath;

            return new OrderItemAdminDto
            {
                Id = orderDetail.Id,
                VariantId = orderDetail.VariantId,
                ProductId = variant?.ProductId ?? 0,
                Name = productTranslation?.Name ?? string.Empty,
                Description = productTranslation?.Description ?? string.Empty,
                ColorName = colorTranslation?.Name ?? string.Empty,
                HexCode = variant?.Color?.HexCode ?? string.Empty,
                SizeName = variant?.Size?.Name ?? string.Empty,
                Quantity = orderDetail.Quantity,
                // Use checkout values so later catalog price changes do not alter the order.
                UnitPrice = orderDetail.UnitPrice,
                Discount = orderDetail.Discount,
                TotalPrice = orderDetail.TotalPrice,
                Image = string.IsNullOrWhiteSpace(imagePath) ? string.Empty : $"{baseUrl.TrimEnd('/')}/{imagePath.TrimStart('/')}"
            };
        }

        public static OrderDetailsDto ToDetailsDto(this OrderDetail orderDetail, int languageId, string baseUrl)
        {
            ArgumentNullException.ThrowIfNull(orderDetail);

            var product = orderDetail.Variant?.Product;
            var productTranslation = orderDetail.Variant?.Product?.Translations?.FirstOrDefault(t => t.LanguageId == languageId);
            var color = orderDetail.Variant?.Color;
            var colorTranslation = orderDetail.Variant?.Color?.Translations?.FirstOrDefault(t => t.LanguageId == languageId);
            var size = orderDetail.Variant?.Size;
            var image = orderDetail.Variant?.Images?.FirstOrDefault()?.MediaAsset?.FilePath;

            return new OrderDetailsDto()
            {
                Name = productTranslation?.Name ?? string.Empty,
                Description = productTranslation?.Description ?? string.Empty,
                Price = product?.Price ?? 0,
                OriginalPrice = product?.OriginalPrice ?? 0,
                ColorName = colorTranslation?.Name ?? string.Empty,
                HexCode = color?.HexCode ?? string.Empty,
                SizeName = size?.Name ?? string.Empty,
                Quantity = orderDetail.Quantity,
                Image = $"{baseUrl}/{image}" ?? string.Empty
            };
        }

        public static List<OrderDetailsDto> ToDetailsDtoList(this IEnumerable<OrderDetail> ordersDetails, int languageId, string baseUrl)
        {
            ArgumentNullException.ThrowIfNull(ordersDetails);

            return ordersDetails.Select(od => od.ToDetailsDto(languageId, baseUrl)).ToList();
        }
    }
}
