using Application.DTOs;
using Application.Interfaces;
using Application.Mapping;
using Application.Responses;
using Core.Entities;
using Core.Enum;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class ColorService : LookupService<Color>, IColorService
    {
        private readonly IServices<Language> _langServices;

        public ColorService(
            IServices<Language> lang,
            IUnitOfWork unitOfWork,
            IRepo<Color> repo,
            ILogger<ColorService> logger) : base(unitOfWork, repo, logger)
        {
            _langServices = lang;
        }


        private async Task<int> GetLanguageId(string langCode)
        {
            _logger.LogInformation("Retrieving language ID for language code: {LangCode}", langCode);
            int langId = await _langServices.Query(l => l.Code == langCode).Select(l => l.Id).FirstOrDefaultAsync();

            if (langId == 0)
            {
                _logger.LogWarning("Language code {LangCode} not found. Defaulting to language ID 1.", langCode);
                return 1; // Default to Arabic if not found
            }

            _logger.LogInformation("Found language ID {LangId} for language code {LangCode}", langId, langCode);
            return langId;
        }

        #region CreateColor Method
        public async Task<ApiResponse<ColorDetailsAdminDto>> CreateColor(ColorDetailsAdminDto dto)
        {
            _logger.LogInformation("CreateColor requested with hex code: {HexCode}", dto.HexCode);

            // Both translations belong to the same color, as in product creation.
            var languages = await LookupLanguageHelper.GetLanguageIdsAsync(_langServices);

            if (languages == null)
            {
                _logger.LogWarning("Cannot create color because Arabic or English language is missing");
                return new()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.NotFound,
                    Message = "Arabic and English languages must be configured before creating a color."
                };
            }

            var (arabicId, englishId) = languages.Value;
            var color = new Color();
            color.FromAdminDetailsDto(dto, arabicId, englishId);

            // A single save keeps the color and its translations atomic.
            Add(color);
            await SaveChangesAsync();

            _logger.LogInformation("Color created successfully with ID: {ColorId}", color.Id);
            return new()
            {
                IsSuccess = true,
                Data = color.ToAdminDetailsDto(arabicId, englishId),
                Message = "Color created successfully."
            };
        }
        #endregion

        #region UpdateColor Method
        public async Task<ApiResponse<ColorDetailsAdminDto>> UpdateColor(int id, ColorDetailsAdminDto dto)
        {
            _logger.LogInformation("UpdateColor requested for ID: {ColorId}", id);

            if (dto.Id.HasValue && dto.Id.Value != id)
            {
                _logger.LogWarning("Color update ID does not match the route ID: {ColorId}", id);
                return new() { IsSuccess = false, ErrorType = ErrorType.ValidationError, Message = "Color ID must match the route ID." };
            }

            // Track only the color and translations needed for editing.
            var color = await GetAsync(c => c.Id == id, includes: c => c.Translations);
            if (color == null)
            {
                _logger.LogWarning("Color with ID: {ColorId} was not found for update", id);
                return new() { IsSuccess = false, ErrorType = ErrorType.NotFound, Message = "Color not found." };
            }

            var languages = await LookupLanguageHelper.GetLanguageIdsAsync(_langServices);
            if (languages == null)
            {
                _logger.LogWarning("Cannot update color because Arabic or English language is missing");
                return new() { IsSuccess = false, ErrorType = ErrorType.NotFound, Message = "Arabic and English languages must be configured before updating a color." };
            }

            var (arabicId, englishId) = languages.Value;
            color.FromAdminDetailsDto(dto, arabicId, englishId);
            return await SaveLookupUpdateAsync(id, "Color", () => color.ToAdminDetailsDto(arabicId, englishId));
        }
        #endregion

        #region DeleteColor Method
        public Task<ApiResponse<int>> DeleteColor(int id) =>
            DeleteUnusedLookupAsync(id, c => !c.Variants.Any(), "Color",
                "Color cannot be deleted because it is used by product variants.");
        #endregion

        public async Task<ApiResponse<(IEnumerable<ColorDto> Items, int TotalCount)>> GetAll(string langCode)
        {
            int langId = await GetLanguageId(langCode);

            _logger.LogInformation("GetAll colors requested for language ID: {LangId}", langId);

            var (items, totalCount) = await GetPagedAsync(includes: c => c.Translations.Where(t => t.LanguageId == langId));
            _logger.LogInformation("Successfully retrieved {Count} colors for language ID: {LangId}", items.Count(), langId);

            var colorDtos = items.ToDtoList(langId);

            return new()
            {
                IsSuccess = true,
                Data = (colorDtos, totalCount),
                Message = "Colors retrieved successfully."
            };
        }

        public async Task<ApiResponse<ColorDetailsDto>> GetById(int colorId, string langCode)
        {
            int langId = await GetLanguageId(langCode);

            _logger.LogInformation("GetColorById requested for ColorID: {ColorId}, LanguageID: {LangId}", colorId, langId);

            var color = await GetByIdAsync(colorId, c => c.Translations.Where(l => l.LanguageId == langId));

            if (color == null)
            {
                _logger.LogWarning("Color with ID: {ColorId} was not found.", colorId);

                return new()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.NotFound,
                    Message = "Color not found."
                };
            }
            _logger.LogInformation("Successfully retrieved color ID: {ColorId} with filtered translation for LanguageID: {LangId}", colorId, langId);

            var colorDetailsDto = color.ToDetailsDto(langId);

            return new()
            {
                IsSuccess = true,
                Data = colorDetailsDto,
                Message = "Color retrieved successfully.",
            };
        }

        public async Task<ApiResponse<IEnumerable<ColorLookupDto>>> Lookup(string langCode)
        {
            int langId = await GetLanguageId(langCode);

            _logger.LogInformation("Color lookup requested for language ID: {LangId}", langId);

            var lookup = await GetAllAsync(includes: c => c.Translations.Where(t => t.LanguageId == langId));
            _logger.LogInformation("Successfully retrieved color lookups for language ID: {LangId}", langId);

            var lookupDto = lookup.ToListLookupDto(langId);

            return new()
            {
                IsSuccess = true,
                Message = "Color lookups retrieved successfully.",
                Data = lookupDto,
            };
        }
    }
}
