using Application.DTOs;
using Application.Responses;
using Core.Entities;
using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IProductService : IServices<Product>
    {
        IQueryable<Product> GetProductsWithIncludes(int languageId);
        Task<ApiResponse<(IEnumerable<Product> Items, int TotalCount)>> GetFilteredProductsAsync(int languageId, ProductFilterDto filterDto);
        Task<ApiResponse<Product>> GetProductById(int id, int langId);
    }
}
