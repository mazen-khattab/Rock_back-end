using Application.DTOs;
using Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Mapping
{
    public static class OrderDetailsMappingExtensions
    {
        public static OrderDetailsDto ToDetailsDto(this OrderDetail orderDetail, int languageId)
        {
            if (orderDetail is null)
            {
                throw new ArgumentNullException(nameof(orderDetail));
            }

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
                Image = image ?? string.Empty
            };
        }

        public static List<OrderDetailsDto> ToDetailsDtoList(this IEnumerable<OrderDetail> ordersDetails, int languageId)
        {
            if (ordersDetails == null)
            {
                throw new ArgumentNullException(nameof(ordersDetails));
            }

            return ordersDetails.Select(od => od.ToDetailsDto(languageId)).ToList();
        }
    }
}
