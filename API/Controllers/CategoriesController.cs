using Application.DTOs;
using Application.Interfaces;
using Application.Responses;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(ICategoryService categoryService, ILogger<CategoriesController> logger)
        {
            _categoryService = categoryService;
            _logger = logger;
        }

        private string GetLangCode()
        {
            _logger.LogInformation("Getting the current language code from the request");
            string currentLang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            _logger.LogInformation("Current language code: {LangCode}", currentLang);

            return currentLang;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            string langCode = GetLangCode();
            _logger.LogInformation("HTTP GET request received for api/categories with langCode: {LangCode}", langCode);

            var result = await _categoryService.GetAll(langCode);

            if (result is not null && result.IsSuccess)
            {
                var (items, totalCount) = result.Data;

                _logger.LogInformation("Successfully mapped and returning {Count} categories for langCode: {LangCode}", totalCount, langCode);

                return Ok(new
                {
                    items,
                    totalCount,
                    pageNumber = 1,
                    pageSize = 12,
                    totalPages = (int)Math.Ceiling(totalCount / (double)12)
                });
            }

            _logger.LogWarning("Failed to retrieve categories in controller for langCode: {LangCode}. Message: {Msg}", langCode, result?.Message ?? "");

            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Failed to retrieve categories.",
                Path = "api/Categories",
                Method = HttpMethods.Get,
                TraceId = HttpContext.TraceIdentifier
            };

            return BadRequest(errorResponse);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            string langCode = GetLangCode();
            _logger.LogInformation("HTTP GET request received for api/categories/{Id} with langCode: {LangCode}", id, langCode);

            var result = await _categoryService.GetById(id, langCode);

            if (result is not null && result.IsSuccess)
            {
                _logger.LogInformation("Successfully mapped and returning category ID: {Id}", id);

                return Ok(new ApiResponse<CategoryDetailsDto>
                {
                    IsSuccess = true,
                    Data = result.Data,
                });
            }

            _logger.LogWarning("Category with ID: {Id} not found or failed to retrieve. Message: {Msg}", id, result?.Message ?? "");

            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Failed to retrieve category by the ID.",
                Path = "api/Categories/{id}",
                Method = HttpMethods.Get,
                TraceId = HttpContext.TraceIdentifier
            };

            return BadRequest(errorResponse);
        }

        [HttpGet("lookup")]
        public async Task<IActionResult> GetLookup()
        {
            var langCode = GetLangCode();

            _logger.LogInformation("HTTP GET request received for api/categories/lookup with language code: {LangCode} from request header", langCode);

            var result = await _categoryService.Lookup(langCode);

            if (result is not null && result.IsSuccess)
            {
                var lookupDto = result.Data;

                _logger.LogInformation("Successfully returning category lookups for dropdown with language code: {LangCode}", langCode);

                return Ok(new ApiResponse<IEnumerable<CategoryLookupDto>>
                {
                    IsSuccess = true,
                    Data = lookupDto,
                });
            }

            _logger.LogWarning("Failed to retrieve category lookups. Message: {Msg}", result?.Message ?? "");

            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Failed to retrieve category lookups.",
                Path = "api/Categories/lookup",
                Method = HttpMethods.Get,
                TraceId = HttpContext.TraceIdentifier
            };

            return BadRequest(errorResponse);
        }
    }
}
