using Application.DTOs;
using Application.Responses;
using Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IProductService
    {
        IQueryable<Product> GetProductsWithIncludes(int languageId);
        Task<ApiResponse<(IEnumerable<ProductsDto> Items, int TotalCount)>> GetFilteredProductsAsync(int languageId, ProductFilterDto filterDto);
        Task<ApiResponse<ProductsDto>> GetProductById(int id, int langId);
    }
}
