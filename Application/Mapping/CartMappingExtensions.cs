using Application.DTOs;
using Core.Entities;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Mapping
{
    public static class CartMappingExtensions
    {
        public static CartDto ToDto(this BaseCart cart, string baseUrl)
        {
            ArgumentNullException.ThrowIfNull(cart);

            var productName = cart.Variant?.Product?.Translations?.FirstOrDefault()?.Name ?? string.Empty;
            var productDescription = cart.Variant?.Product?.Translations?.FirstOrDefault()?.Description ?? string.Empty;
            var productPrice = cart.Variant?.Product?.Price ?? 0;
            var productId = cart.Variant?.ProductId ?? 0;
            var colorName = cart.Variant?.Color?.Translations?.FirstOrDefault()?.Name ?? string.Empty;
            var hexCode = cart.Variant?.Color?.HexCode ?? string.Empty;
            var sizeName = cart.Variant?.Size?.Name ?? string.Empty;
            var reserved = cart.Variant?.Reserved ?? 0;

            var dto = new CartDto()
            {
                Id = cart.Id,
                VariantId = cart.VariantId,
                ProductId = productId,
                Name = productName,
                Description = productDescription,
                Price = productPrice,
                imagesDtos = cart.Variant?.Images?.FirstOrDefault()?.ToDto(baseUrl) ?? new ImagesDto(),
                Color = colorName,
                HexCode = hexCode,
                Size = sizeName,
                Reserved = reserved,
                Quantity = cart.Quantity,
            };

            return dto;
        }

        public static List<CartDto> ToDtoList(this IEnumerable<BaseCart> carts, string baseUrl)
        {
            ArgumentNullException.ThrowIfNull(carts);

            return carts.Select(c => c.ToDto(baseUrl)).ToList();
        }
    }
}
