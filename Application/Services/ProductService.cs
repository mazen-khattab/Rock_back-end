using Application.DTOs;
using Application.Interfaces;
using Application.Mapping;
using Application.Responses;
using Core.Entities;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public class ProductService : Services<Product>, IProductService
    {
        public ProductService(IUnitOfWork unitOfWork, IRepo<Product> productRepo, ILogger<ProductService> logger) : base(unitOfWork, productRepo, logger) { }

        public IQueryable<Product> GetProductsWithIncludes(int languageId)
        {
            _logger.LogDebug("Building product query for language {LanguageId}", languageId);

            var products = Query(filter: p => p.Translations.Any(t => t.LanguageId == languageId), tracked: false)
            .Include(p => p.Category)
                .ThenInclude(c => c.Translations.Where(t => t.LanguageId == languageId))
            .Include(p => p.Translations.Where(t => t.LanguageId == languageId))
            .Include(p => p.Variants)
                .ThenInclude(v => v.Images)
                    .ThenInclude(i => i.MediaAsset)
            .Include(p => p.Variants)
                .ThenInclude(v => v.Size)
            .Include(p => p.Variants)
                .ThenInclude(v => v.Color)
                    .ThenInclude(c => c.Translations.Where(t => t.LanguageId == languageId));

            return products;
        }

        public async Task<ApiResponse<(IEnumerable<Product> Items, int TotalCount)>> GetFilteredProductsAsync(int languageId, ProductFilterDto filterDto)
        {
            var query = GetProductsWithIncludes(languageId);

            // Apply filters
            query = ApplyFilters(query, filterDto);

            // Get count
            int totalCount = await query.CountAsync();

            // Apply pagination
            var pageNumber = Math.Max(1, filterDto.PageNumber);
            var pageSize = Math.Clamp(filterDto.PageSize <= 0 ? 12 : filterDto.PageSize, 1, 50);

            var items = await query
                .OrderBy(p => p.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new ApiResponse<(IEnumerable<Product> Items, int TotalCount)>()
            {
                IsSucess = true,
                Data = (items,  totalCount)
            };
        }

        private IQueryable<Product> ApplyFilters(IQueryable<Product> query, ProductFilterDto filter)
        {
            if (!string.IsNullOrEmpty(filter.Category))
            {
                query = query.Where(p =>
                    p.Category.Translations.Any(t => t.Name.Contains(filter.Category)));
            }

            if (!string.IsNullOrEmpty(filter.Color))
            {
                query = query.Where(p =>
                    p.Variants.Any(v => v.Color.Translations.Any(t => t.Name.Contains(filter.Color))));
            }

            if (!string.IsNullOrEmpty(filter.Size))
            {
                query = query.Where(p =>
                    p.Variants.Any(v => v.Size.Name.Contains(filter.Size)));
            }

            return query;
        }

        public async Task<ApiResponse<Product>> GetProductById(int id, int langId)
        {
            var productWithIncludes = GetProductsWithIncludes(langId);

            var product = await productWithIncludes.FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return new ApiResponse<Product>()
                {
                    IsSucess = false,
                    Message = "product not found",
                    Data = null
                };
            }

            return new ApiResponse<Product>()
            {
                IsSucess = true,
                Data = product,
            };
        }
    }
}
