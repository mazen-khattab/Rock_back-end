using Application.DTOs;
using Application.Interfaces;
using Application.Responses;
using Core.Entities;
using Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public class CategoryService : Services<Category>, ICategoryService
    {
        public CategoryService(IUnitOfWork unitOfWork, IRepo<Category> repo, ILogger<CategoryService> logger) : base(unitOfWork, repo, logger) { }

        public async Task<ApiResponse<(IEnumerable<Category> Items, int TotalCount)>> GetAll(int langId)
        {
            _logger.LogInformation("GetAll categories requested for language ID: {LangId}", langId);

            try
            {
                var (items, totalCount) = await GetPagedAsync(includes: c => c.Translations.Where(t => t.LanguageId == langId));

                _logger.LogInformation("Successfully retrieved {Count} categories for language ID: {LangId}", totalCount, langId);
                return new()
                {
                    IsSuccess = true,
                    Data = (items, totalCount),
                    Message = "Categories retrieved successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching all categories for language ID: {LangId}", langId);

                return new()
                {
                    IsSuccess = false,
                    Message = "An error occurred while retrieving categories."
                };
            }
        }

        public async Task<ApiResponse<Category>> GetById(int categoryId, int langId)
        {
            _logger.LogInformation("GetCategoryById requested for CategoryID: {CategoryId}, LanguageID: {LangId}", categoryId, langId);

            try
            {
                var category = await GetByIdAsync(categoryId, c => c.Translations.Where(l => l.LanguageId == langId));

                if (category == null)
                {
                    _logger.LogWarning("Category with ID: {CategoryId} was not found.", categoryId);

                    return new()
                    {
                        IsSuccess = false,
                        Message = "Category not found."
                    };
                }

                _logger.LogInformation("Successfully retrieved category ID: {CategoryId} with filtered translation for LanguageID: {LangId}", categoryId, langId);

                return new()
                {
                    IsSuccess = true,
                    Data = category,
                    Message = "Category retrieved successfully.",
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting category {CategoryId} for language {LangId}", categoryId, langId);
                return new()
                {
                    IsSuccess = false,
                    Message = "An error occurred while retrieving the category."
                };
            }
        }

        public async Task<ApiResponse<IEnumerable<Category>>> Lookup(int langId)
        {
            _logger.LogInformation("Category lookup requested for language ID: {LangId}", langId);

            try
            {
                var lookup = await GetAllAsync(includes: c => c.Translations.Where(t => t.LanguageId == langId));

                _logger.LogInformation("Successfully retrieved category lookups for language ID: {LangId}", langId);

                return new()
                {
                    IsSuccess = true,
                    Message = "Category lookups retrieved successfully.",
                    Data = lookup,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during category lookup for language {LangId}", langId);
                return new()
                {
                    IsSuccess = false,
                    Message = "An error occurred while retrieving category lookups."
                };
            }
        }
    }
}
