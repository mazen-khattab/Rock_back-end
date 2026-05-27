using Application.Interfaces;
using Application.Responses;
using Core.Entities;
using Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class ColorService : Services<Color>, IColorService
    {
        public ColorService(IUnitOfWork unitOfWork, IRepo<Color> repo, ILogger<ColorService> logger) : base(unitOfWork, repo, logger) { }

        public async Task<ApiResponse<(IEnumerable<Color> Items, int TotalCount)>> GetAll(int langId)
        {
            _logger.LogInformation("GetAll colors requested for language ID: {LangId}", langId);

            try
            {
                var (items, totalCount) = await GetPagedAsync(includes: c => c.Translations.Where(t => t.LanguageId == langId));

                _logger.LogInformation("Successfully retrieved {Count} colors for language ID: {LangId}", items.Count(), langId);

                return new()
                {
                    IsSuccess = true,
                    Data = (items, totalCount),
                    Message = "Colors retrieved successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching all colors for language ID: {LangId}", langId);

                return new()
                {
                    IsSuccess = false,
                    Message = "An error occurred while retrieving colors."
                };
            }
        }

        public async Task<ApiResponse<Color>> GetById(int colorId, int langId)
        {
            _logger.LogInformation("GetColorById requested for ColorID: {ColorId}, LanguageID: {LangId}", colorId, langId);

            try
            {
                var color = await GetByIdAsync(colorId, c => c.Translations.Where(l => l.LanguageId == langId));

                if (color == null)
                {
                    _logger.LogWarning("Color with ID: {ColorId} was not found.", colorId);

                    return new()
                    {
                        IsSuccess = false,
                        Message = "Color not found."
                    };
                }

                _logger.LogInformation("Successfully retrieved color ID: {ColorId} with filtered translation for LanguageID: {LangId}", colorId, langId);

                return new()
                {
                    IsSuccess = true,
                    Data = color,
                    Message = "Color retrieved successfully.",
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting color {ColorId} for language {LangId}", colorId, langId);
                return new()
                {
                    IsSuccess = false,
                    Message = "An error occurred while retrieving the color."
                };
            }
        }

        public async Task<ApiResponse<IEnumerable<Color>>> Lookup(int langId)
        {
            _logger.LogInformation("Color lookup requested for language ID: {LangId}", langId);

            try
            {
                var lookup = await GetAllAsync(includes: c => c.Translations.Where(t => t.LanguageId == langId));

                _logger.LogInformation("Successfully retrieved color lookups for language ID: {LangId}", langId);

                return new()
                {
                    IsSuccess = true,
                    Message = "Color lookups retrieved successfully.",
                    Data = lookup,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching color lookups for language ID: {LangId}", langId);

                return new()
                {
                    IsSuccess = false,
                    Message = "An error occurred while retrieving color lookups."
                };
            }
        }
    }
}
