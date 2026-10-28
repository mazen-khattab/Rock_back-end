using Application.DTOs;
using Application.Responses;
using Core.Entities;
using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Application.Interfaces
{
    public interface IProductService : IServices<Product>
    {
        Task<ApiResponse<(IEnumerable<ProductsDto> Items, int TotalCount)>> GetAllProducts(ProductFilterDto filterDto, string langCode, string baseUrl);
        Task<ApiResponse<(IEnumerable<ProductsAdminDto> Items, int TotalCount)>> GetAllAdminProducts(ProductFilterDto filterDto, string langCode, string baseUrl);
        Task<ApiResponse<ProductDetailsDto>> GetProductById(int id, string langCode, string baseUrl);
        Task<ApiResponse<ProductDetailsAdminDto>> GetAdminProductbyId(int id, string baseUrl);
        Task<ApiResponse<ProductDetailsAdminDto>> CreateProduct(ProductDetailsAdminDto productDto, string baseUrl);
        Task<ApiResponse<ProductDetailsAdminDto>> UpdateProduct(int id, ProductDetailsAdminDto productDto, string baseUrl);
        Task<ApiResponse<int>> DeleteProduct(int id);
    }
}
