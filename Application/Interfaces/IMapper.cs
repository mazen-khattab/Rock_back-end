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
        List<ProductsDto> MapToDtoList(IEnumerable<Product> products, int languageId);
        List<VariantDto> MapToDtoList(IEnumerable<Variant> variants, int languageId);
        List<VariantImagesDto> MapToDtoList(IEnumerable<VariantImage> variantImages);
        List<CartDto> MapToDtoList(IEnumerable<BaseCart> carts);

        // Map from Entity to Dto
        ProductsDto MapToDto(Product product, int languageId);
        VariantDto MapToDto(Variant variant, int languageId);
        VariantImagesDto MapToDto(VariantImage variantImage);
        CartDto MapToDto(BaseCart cart);
    }
}
