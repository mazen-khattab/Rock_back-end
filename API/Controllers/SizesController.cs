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
    public class SizesController : ControllerBase
    {
        private readonly ISizeService _sizeService;
        private readonly ILogger<SizesController> _logger;

        public SizesController(ISizeService sizeService, ILogger<SizesController> logger)
        {
            _sizeService = sizeService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("HTTP GET request received for api/sizes");

            var result = await _sizeService.GetAll();

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to retrieve sizes in controller. Message: {Msg}", result.Message);
                return BadRequest(new { error = result.Message });
            }

            var (items, totalCount) = result.Data;
            var sizeDtos = items.ToDtoList();

            _logger.LogInformation("Successfully mapped and returning {Count} sizes", totalCount);

            return Ok(new
            {
                items = sizeDtos,
                totalCount = totalCount,
                pageNumber = 1,
                pageSize = 12,
                totalPages = (int)Math.Ceiling(totalCount / (double)12)
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            _logger.LogInformation("HTTP GET request received for api/sizes/{Id}", id);

            var result = await _sizeService.GetById(id);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Size with ID: {Id} not found or failed to retrieve. Message: {Msg}", id, result.Message);
                return BadRequest(new { error = result.Message });
            }

            var sizeDto = result.Data?.ToDetailsDto() ?? new();

            _logger.LogInformation("Successfully mapped and returning size ID: {Id}", id);

            return Ok(new ApiResponse<SizeDetailsDto>
            {
                IsSuccess = true,
                Data = sizeDto,
            });
        }

        [HttpGet("lookup")]
        public async Task<IActionResult> GetLookup()
        {
            _logger.LogInformation("HTTP GET request received for api/sizes/lookup");

            var result = await _sizeService.Lookup();

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to retrieve size lookups. Message: {Msg}", result.Message);
                return BadRequest(new { error = result.Message });
            }

            var lookupDto = result.Data?.ToListLookupDto() ?? new List<SizeLookupDto>();

            _logger.LogInformation("Successfully returning size lookups for dropdown.");

            return Ok(new ApiResponse<IEnumerable<SizeLookupDto>>
            {
                IsSuccess = true,
                Data = lookupDto,
            });
        }
    }
}
