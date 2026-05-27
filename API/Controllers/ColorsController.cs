using Application.DTOs;
using Application.Interfaces;
using Application.Mapping;
using Application.Responses;
using Core.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int langId)
        {
            _logger.LogInformation("HTTP GET request received for api/colors with langId: {LangId}", langId);

            var result = await _colorService.GetAll(langId);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to retrieve colors in controller for langId: {LangId}. Message: {Msg}", langId, result.Message);
                return BadRequest(new { error = result.Message });
            }

            var (items, totalCount) = result.Data;
            var colorDtos = items.ToDtoList(langId);

            _logger.LogInformation("Successfully mapped and returning {Count} colors", totalCount);

            return Ok(new
            {
                items = colorDtos,
                totalCount = totalCount,
                pageNumber = 1,
                pageSize = 12,
                totalPages = (int)Math.Ceiling(totalCount / (double)12)
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id, [FromQuery] int langId)
        {
            _logger.LogInformation("HTTP GET request received for api/colors/{Id} with langId: {LangId}", id, langId);

            var result = await _colorService.GetById(id, langId);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Color with ID: {Id} not found or failed to retrieve. Message: {Msg}", id, result.Message);
                return BadRequest(new { error = result.Message });
            }

            var colorDto = result.Data?.ToDetailsDto(langId) ?? new();

            _logger.LogInformation("Successfully mapped and returning color ID: {Id}", id);

            return Ok(new ApiResponse<Color>
            {
                IsSuccess = true,
                Data = result.Data,
                Message = result.Message
            });
        }

        [HttpGet("lookup/{langId}")]
        public async Task<IActionResult> GetLookup(int langId)
        {
            _logger.LogInformation("HTTP GET request received for api/colors/lookup with langId: {LangId}", langId);

            var result = await _colorService.Lookup(langId);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to retrieve color lookups. Message: {Msg}", result.Message);
                return BadRequest(new { error = result.Message });
            }

            var lookupDto = result.Data?.ToListLookupDto(langId) ?? new List<ColorLookupDto>();

            _logger.LogInformation("Successfully returning color lookups for dropdown.");

            return Ok(new ApiResponse<IEnumerable<ColorLookupDto>>
            {
                IsSuccess = true,
                Data = lookupDto,
            });
        }
    }
}
