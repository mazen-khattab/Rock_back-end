using Application.DTOs;
using Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IMapper
    {
        // Map from Entity List to Dto List
        List<ProductDto> MapToDtoList(IEnumerable<Product> products, int languageId);
        List<VariantDto> MapToDtoList(IEnumerable<Variant> variants, int languageId);
        List<VariantImagesDto> MapToDtoList(IEnumerable<VariantImage> variantImages);
        List<CartDto> MapToDtoList(IEnumerable<BaseCart> carts);
        List<OrderDto> MapToDtoList(IEnumerable<Order> orders, int languageId);
        List<OrderDetailsDto> MapToDtoList(IEnumerable<OrderDetail> ordersDetails, int languageId);

        // Map from Entity to Dto
        ProductDto MapToDto(Product product, int languageId);
        VariantDto MapToDto(Variant variant, int languageId);
        VariantImagesDto MapToDto(VariantImage variantImage);
        CartDto MapToDto(BaseCart cart);
        OrderDto MapToDto(Order order, int languageId);
        OrderDetailsDto MapToDto(OrderDetail orderDetail, int languageId);
    }
}
