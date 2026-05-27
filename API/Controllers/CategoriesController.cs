using Application.DTOs;
using Application.Interfaces;
using Application.Mapping;
using Application.Responses;
using Azure;
using Core.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int langId)
        {
            _logger.LogInformation("HTTP GET request received for api/categories with langId: {LangId}", langId);

            var result = await _categoryService.GetAll(langId);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to retrieve categories in controller for langId: {LangId}. Message: {Msg}", langId, result.Message);
                return BadRequest(new { error = result.Message });
            }

            var (items, totalCount) = result.Data;

            var categoryDtos = items?.ToDtoList(langId) ?? new();

            _logger.LogInformation("Successfully mapped and returning {Count} categories for langId: {LangId}", totalCount, langId);

            return Ok(new
            {
                items = categoryDtos,
                totalCount = totalCount,
                pageNumber = 1,
                pageSize = 12,
                totalPages = (int)Math.Ceiling(totalCount / (double)12)
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id, [FromQuery] int langId)
        {
            _logger.LogInformation("HTTP GET request received for api/categories/{Id} with langId: {LangId}", id, langId);

            var result = await _categoryService.GetById(id, langId);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Category with ID: {Id} not found or failed to retrieve. Message: {Msg}", id, result.Message);
                return BadRequest(new { error = result.Message });
            }

            var categoryDto = result.Data?.ToDetailsDto(langId) ?? new();

            _logger.LogInformation("Successfully mapped and returning category ID: {Id}", id);

            return Ok(new ApiResponse<categoryDetailsDto>
            {
                IsSuccess = true,
                Data = categoryDto,
            });
        }

        [HttpGet("lookup/{langId}")]
        public async Task<IActionResult> GetLookup(int langId)
        {
            _logger.LogInformation("HTTP GET request received for api/categories/lookup with langId: {LangId}", langId);

            var result = await _categoryService.Lookup(langId);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to retrieve category lookups. Message: {Msg}", result.Message);
                return BadRequest(new { error = result.Message });
            }

            var lookupDto = result.Data?.ToListLookupDto(langId) ?? new List<CategoryLookupDto>();

            _logger.LogInformation("Successfully returning category lookups for dropdown.");

            return Ok(new ApiResponse<List<CategoryLookupDto>>
            {
                IsSuccess = true,
                Data = lookupDto,
            });
        }
    }
}
