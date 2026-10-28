using Application.DTOs;
using Application.Interfaces;
using Application.Responses;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ColorsController : ControllerBase
    {
        private readonly IColorService _colorService;
        private readonly ILogger<ColorsController> _logger;

        public ColorsController(IColorService colorService, ILogger<ColorsController> logger)
        {
            _colorService = colorService;
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
            _logger.LogInformation("HTTP GET request received for api/colors with langCode: {LangCode}", langCode);

            var result = await _colorService.GetAll(langCode);

            if (result is not null && result.IsSuccess)
            {
                var (items, totalCount) = result.Data;

                _logger.LogInformation("Successfully mapped and returning {Count} colors", totalCount);

                return Ok(new
                {
                    items,
                    totalCount,
                    pageNumber = 1,
                    pageSize = 12,
                    totalPages = (int)Math.Ceiling(totalCount / (double)12)
                });
            }

            _logger.LogError("Failed to retrieve colors in controller for langCode: {LangCode}. Message: {Msg}", langCode, result?.Message ?? "");

            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Failed to retrieve colors.",
                Path = "api/Colors/getAll",
                Method = HttpMethods.Get,
                TraceId = HttpContext.TraceIdentifier
            };

            return BadRequest(errorResponse);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            string langCode = GetLangCode();
            _logger.LogInformation("HTTP GET request received for api/colors/{Id} with langCode: {LangCode}", id, langCode);

            var result = await _colorService.GetById(id, langCode);

            if (result is not null && result.IsSuccess)
            {
                var colorDetailsDto = result.Data;

                _logger.LogInformation("Successfully mapped and returning color ID: {Id}", id);

                return Ok(new ApiResponse<ColorDetailsDto>
                {
                    IsSuccess = true,
                    Data = colorDetailsDto,
                    Message = result.Message
                });
            }

            _logger.LogWarning("Color with ID: {Id} not found or failed to retrieve. Message: {Msg}", id, result?.Message ?? "");

            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Failed to retrieve color by id.",
                Path = "api/Colors/{id}",
                Method = HttpMethods.Get,
                TraceId = HttpContext.TraceIdentifier
            };

            return BadRequest(errorResponse);
        }

        [HttpGet("lookup")]
        public async Task<IActionResult> GetLookup()
        {
            string langCode = GetLangCode();
            _logger.LogInformation("HTTP GET request received for api/colors/lookup with langCode: {LangCode}", langCode);

            var result = await _colorService.Lookup(langCode);

            if (result is not null && result.IsSuccess)
            {
                var lookupDto = result.Data;

                _logger.LogInformation("Successfully returning color lookups for dropdown.");

                return Ok(new ApiResponse<IEnumerable<ColorLookupDto>>
                {
                    IsSuccess = true,
                    Data = lookupDto,
                });
            }

            _logger.LogWarning("Failed to retrieve color lookups. Message: {Msg}", result?.Message ?? "");

            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Failed to retrieve lookup colors.",
                Path = "api/Colors/lookup",
                Method = HttpMethods.Get,
                TraceId = HttpContext.TraceIdentifier
            };

            return BadRequest(errorResponse);
        }
    }
}
