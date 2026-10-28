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

            if (result is not null && result.IsSuccess)
            {
                var (items, totalCount) = result.Data;

                _logger.LogInformation("Successfully mapped and returning {Count} sizes", totalCount);

                return Ok(new
                {
                    items,
                    totalCount,
                    pageNumber = 1,
                    pageSize = 12,
                    totalPages = (int)Math.Ceiling(totalCount / (double)12)
                });
            }

            _logger.LogWarning("Failed to retrieve sizes in controller. Message: {Msg}", result?.Message ?? "");

            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Failed to retrieve sizes.",
                Path = "api/Sizes",
                Method = HttpMethods.Get,
                TraceId = HttpContext.TraceIdentifier
            };

            _logger.LogWarning("Returning error response with StatusCode: {StatusCode}, Message: {Message}", errorResponse.StatusCode, errorResponse.Message);

            return BadRequest(errorResponse);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            _logger.LogInformation("HTTP GET request received for api/sizes/{Id}", id);

            var result = await _sizeService.GetById(id);

            if (result is not null && result.IsSuccess)
            {
                var sizeDto = result.Data;

                _logger.LogInformation("Successfully mapped and returning size ID: {Id}", id);

                return Ok(new ApiResponse<SizeDetailsDto>
                {
                    IsSuccess = true,
                    Data = sizeDto,
                });
            }

            _logger.LogWarning("Size with ID: {Id} not found or failed to retrieve. Message: {Msg}", id, result?.Message ?? "");
            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Failed to retrieve size with id.",
                Path = "api/Sizes/{id}",
                Method = HttpMethods.Get,
                TraceId = HttpContext.TraceIdentifier
            };

            _logger.LogWarning("Returning error response with StatusCode: {StatusCode}, Message: {Message}", errorResponse.StatusCode, errorResponse.Message);

            return BadRequest(errorResponse);
        }

        [HttpGet("lookup")]
        public async Task<IActionResult> GetLookup()
        {
            _logger.LogInformation("HTTP GET request received for api/sizes/lookup");

            var result = await _sizeService.Lookup();

            if (result is not null && result.IsSuccess)
            {
                var lookupDto = result.Data;

                _logger.LogInformation("Successfully returning size lookups for dropdown.");

                return Ok(new ApiResponse<IEnumerable<SizeLookupDto>>
                {
                    IsSuccess = true,
                    Data = lookupDto,
                });
            }

            _logger.LogWarning("Failed to retrieve size lookups. Message: {Msg}", result?.Message ?? "");
            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Failed to retrieve size lookups.",
                Path = "api/Sizes/lookup",
                Method = HttpMethods.Get,
                TraceId = HttpContext.TraceIdentifier
            };

            _logger.LogWarning("Returning error response with StatusCode: {StatusCode}, Message: {Message}", errorResponse.StatusCode, errorResponse.Message);

            return BadRequest(errorResponse);
        }
    }
}
