using Application.DTOs;
using Core.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IMapper
    {
        // Map from Entity List to Dto List
        List<ProductsDto> MapToDtoList(IEnumerable<Product> products, int languageId);
        List<VariantsDto> MapToDtoList(IEnumerable<Variant> variants, int languageId);
        List<VariantDetailsDto> MapToDtoList(int languageId, IEnumerable<Variant> variants);
        List<ImagesDto> MapToDtoList(IEnumerable<VariantImage> variantImages);
        List<CartDto> MapToDtoList(IEnumerable<BaseCart> carts);
        List<OrderHistoryDto> MapToDtoList(IEnumerable<Order> orders, int languageId);
        List<OrderDetailsDto> MapToDtoList(IEnumerable<OrderDetail> ordersDetails, int languageId);
        List<ProductsAdminDto> MapToDtoAdminList(IEnumerable<Product> products, int languageId);


        // Map from Entity to Dto
        ProductsDto MapToDto(Product product, int languageId);
        VariantsDto MapToDto(Variant variant, int languageId);
        ProductDetailsDto MapToDto(int languageId, Product product);
        VariantDetailsDto MapToDto(int languageId, Variant variant);
        ImagesDto MapToDto(VariantImage variantImage);
        CartDto MapToDto(BaseCart cart);
        OrderHistoryDto MapToDto(Order order, int languageId);
        OrderDetailsDto MapToDto(OrderDetail orderDetail, int languageId);
        UserProfileDto MapToDto(User user);
        ProductsAdminDto MapToAdminDto(Product product, int languageId);
    }
}
