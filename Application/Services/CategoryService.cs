using Application.DTOs;
using Application.Interfaces;
using Application.Mapping;
using Application.Responses;
using Core.Entities;
using Core.Enum;
using Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class CategoryService : Services<Category>, ICategoryService
    {
        private readonly IServices<Language> _langServices;

        public CategoryService(IUnitOfWork unitOfWork, IRepo<Category> repo, ILogger<CategoryService> logger, IServices<Language> langServices) : base(unitOfWork, repo, logger)
        {
            _langServices = langServices;
        }


        private int GetLanguageId(string langCode)
        {
            _logger.LogInformation("Retrieving language ID for language code: {LangCode}", langCode);
            int langId = _langServices.Query(l => l.Code == langCode).Select(l => l.Id).FirstOrDefault();

            if (langId == 0)
            {
                _logger.LogWarning("Language code {LangCode} not found. Defaulting to language ID 1.", langCode);
                return 1; // Default to Arabic if not found
            }

            _logger.LogInformation("Found language ID {LangId} for language code {LangCode}", langId, langCode);
            return langId;
        }

        #region GetAll Method
        public async Task<ApiResponse<(IEnumerable<CategoryDto> Items, int TotalCount)>> GetAll(string langCode)
        {
            int langId = GetLanguageId(langCode);

            _logger.LogInformation("GetAll categories requested for language ID: {LangId}", langId);

            var (items, totalCount) = await FetchAllCategoriesAsync(langId);
            _logger.LogInformation("Successfully retrieved {Count} categories for language ID: {LangId}", totalCount, langId);

            var itemsDto = items.ToDtoList(langId) ?? new();

            return new()
            {
                IsSuccess = true,
                Data = (itemsDto, totalCount),
                Message = "Categories retrieved successfully."
            };
        }

        public async Task<(IEnumerable<Category>, int)> FetchAllCategoriesAsync(int langId)
        {
            _logger.LogDebug("Fetching all categories from database for language ID: {LangId}", langId);
            return await GetPagedAsync(includes: c => c.Translations.Where(t => t.LanguageId == langId));
        }
        #endregion

        #region GetById Method
        public async Task<ApiResponse<CategoryDetailsDto>> GetById(int categoryId, string langCode)
        {
            int langId = GetLanguageId(langCode);

            _logger.LogInformation("GetCategoryById requested for CategoryID: {CategoryId}, LanguageID: {LangId}", categoryId, langId);

            var category = await FetchCategoryByIdAsync(categoryId, langId);

                if (category == null)
            {
                _logger.LogWarning("Category with ID: {CategoryId} was not found.", categoryId);
                return new() { IsSuccess = false, ErrorType = ErrorType.NotFound, Message = "Category not found." };
            }

            _logger.LogInformation("Successfully retrieved category ID: {CategoryId} with filtered translation for LanguageID: {LangId}", category.Id, langId);

            var categoryDetailsDto = category.ToDetailsDto(langId) ?? new();

            return new()
            {
                IsSuccess = true,
                Data = categoryDetailsDto,
                Message = "Category retrieved successfully."
            };
        }

        public async Task<Category?> FetchCategoryByIdAsync(int categoryId, int langId)
        {
            _logger.LogDebug("Fetching category {CategoryId} from database for language ID: {LangId}", categoryId, langId);
            return await GetByIdAsync(categoryId, c => c.Translations.Where(l => l.LanguageId == langId));
        }
        #endregion

        #region Lookup Method
        public async Task<ApiResponse<IEnumerable<CategoryLookupDto>>> Lookup(string langCode)
        {
            _logger.LogInformation("Category lookup requested for language code: {LangCode}", langCode);

            int langId = GetLanguageId(langCode);
            var lookup = await FetchCategoryLookupsAsync(langId);

            var lookupDto = lookup.ToListLookupDto(langId) ?? new();

            _logger.LogInformation("Successfully retrieved category lookups for language code: {LangCode}", langCode);
            return new()
            {
                IsSuccess = true,
                Message = "Category lookups retrieved successfully.",
                Data = lookupDto
            };
        }

        private async Task<IEnumerable<Category>> FetchCategoryLookupsAsync(int langId)
        {
            _logger.LogDebug("Fetching category lookups from database for language ID: {LangId}", langId);
            return await GetAllAsync(includes: c => c.Translations.Where(t => t.LanguageId == langId));
        }
        #endregion
    }
}
