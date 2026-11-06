using Application.DTOs;
using Application.Interfaces;
using Application.Mapping;
using Application.Responses;
using Core.Entities;
using Core.Enum;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class CategoryService : LookupService<Category>, ICategoryService
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

        #region CreateCategory Method
        public async Task<ApiResponse<CategoryDetailsAdminDto>> CreateCategory(CategoryDetailsAdminDto dto)
        {
            _logger.LogInformation("CreateCategory requested");

            // Resolve language IDs from the database instead of assuming fixed seed IDs.
            var languages = await LookupLanguageHelper.GetLanguageIdsAsync(_langServices);

            if (languages == null)
            {
                _logger.LogWarning("Cannot create category because Arabic or English language is missing");
                return new()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.NotFound,
                    Message = "Arabic and English languages must be configured before creating a category."
                };
            }

            var (arabicId, englishId) = languages.Value;
            var category = new Category();
            category.FromAdminDetailsDto(dto, arabicId, englishId);

            // Save the category and both translations together in one database operation.
            Add(category);
            await SaveChangesAsync();

            _logger.LogInformation("Category created successfully with ID: {CategoryId}", category.Id);
            return new()
            {
                IsSuccess = true,
                Data = category.ToAdminDetailsDto(arabicId, englishId),
                Message = "Category created successfully."
            };
        }
        #endregion

        #region UpdateCategory Method
        public async Task<ApiResponse<CategoryDetailsAdminDto>> UpdateCategory(int id, CategoryDetailsAdminDto dto)
        {
            _logger.LogInformation("UpdateCategory requested for ID: {CategoryId}", id);

            if (dto.Id.HasValue && dto.Id.Value != id)
            {
                _logger.LogWarning("Category update ID does not match the route ID: {CategoryId}", id);
                return new() { IsSuccess = false, ErrorType = ErrorType.ValidationError, Message = "Category ID must match the route ID." };
            }

            // Track only the category and its translations; products and offers are not needed.
            var category = await GetAsync(c => c.Id == id, includes: c => c.Translations);
            if (category == null)
            {
                _logger.LogWarning("Category with ID: {CategoryId} was not found for update", id);
                return new() { IsSuccess = false, ErrorType = ErrorType.NotFound, Message = "Category not found." };
            }

            var languages = await LookupLanguageHelper.GetLanguageIdsAsync(_langServices);
            if (languages == null)
            {
                _logger.LogWarning("Cannot update category because Arabic or English language is missing");
                return new() { IsSuccess = false, ErrorType = ErrorType.NotFound, Message = "Arabic and English languages must be configured before updating a category." };
            }

            var (arabicId, englishId) = languages.Value;
            // Map only editable fields, preserving IDs, audit fields and unrelated translations.
            category.FromAdminDetailsDto(dto, arabicId, englishId);
            return await SaveLookupUpdateAsync(id, "Category", () => category.ToAdminDetailsDto(arabicId, englishId));
        }
        #endregion

        #region DeleteCategory Method
        public Task<ApiResponse<int>> DeleteCategory(int id) =>
            DeleteUnusedLookupAsync(id, c => !c.Products.Any(), "Category",
                "Category cannot be deleted because it is used by products, including soft-deleted products.");
        #endregion

        #region GetAll Method
        public async Task<ApiResponse<(IEnumerable<CategoryDto> Items, int TotalCount)>> GetAll()
        {
            _logger.LogInformation("GetAll categories requested with Arabic and English names");

            var query = Query(tracked: false);
            int totalCount = await query.CountAsync();
            // Match the page size exposed by the existing controller response.
            var items = await query.OrderBy(c => c.Id).Take(12).ProjectToDto().ToListAsync();

            _logger.LogInformation("Retrieved {Count} categories out of {TotalCount} with both names", items.Count, totalCount);

            return new()
            {
                IsSuccess = true,
                Data = (items, totalCount),
                Message = "Categories retrieved successfully."
            };
        }

        #endregion

        #region GetById Method
        public async Task<ApiResponse<CategoryDetailsDto>> GetById(int categoryId)
        {
            _logger.LogInformation("GetCategoryById requested for ID: {CategoryId} with Arabic and English names", categoryId);

            var category = await Query(c => c.Id == categoryId, tracked: false)
                .ProjectToDetailsDto().FirstOrDefaultAsync();

            if (category == null)
            {
                _logger.LogWarning("Category with ID: {CategoryId} was not found.", categoryId);
                return new() { IsSuccess = false, ErrorType = ErrorType.NotFound, Message = "Category not found." };
            }

            _logger.LogInformation("Retrieved category ID: {CategoryId} with both names", categoryId);

            return new()
            {
                IsSuccess = true,
                Data = category,
                Message = "Category retrieved successfully."
            };
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
