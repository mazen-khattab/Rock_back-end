using Application.DTOs;
using Application.Interfaces;
using Application.Responses;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IProductService productService, ILogger<ProductsController> logger)
        {
            _productService = productService;
            _logger = logger;
        }


        private string BaseURL()
        {
            var baseURL = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            _logger.LogInformation("Base URL for product images: {BaseUrl}", baseURL);

            return baseURL;
        }

        private string GetLangCode()
        {
            _logger.LogInformation("Getting the current language code from the request");
            string currentLang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            _logger.LogInformation("Current language code: {LangCode}", currentLang);

            return currentLang;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProducts([FromQuery] ProductFilterDto filterDto)
        {
            _logger.LogInformation("GetAllProducts controller called with PageNumber: {PageNumber}, PageSize: {PageSize}", filterDto.PageNumber, filterDto.PageSize);

            var baseUrl = BaseURL();
            string langCode = GetLangCode();

            _logger.LogDebug("BaseUrl: {BaseUrl}, LangCode: {LangCode}", baseUrl, langCode);

            var result = await _productService.GetAllProducts(filterDto, langCode, baseUrl);

            _logger.LogDebug("Service call completed. IsSuccess: {IsSuccess}", result?.IsSuccess);

            if (result is not null && result.IsSuccess)
            {
                var (items, totalCount) = result.Data;

                _logger.LogInformation("Successfully retrieved {ItemCount} products out of {TotalCount} total for LangCode: {LangCode}",
                    items?.Count() ?? 0, totalCount, langCode);

                return Ok(new
                {
                    items,
                    totalCount,
                    pageNumber = filterDto.PageNumber,
                    pageSize = filterDto.PageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)filterDto.PageSize)
                });
            }

            _logger.LogError("Failed to retrieve colors in controller for langCode: {LangCode}. Message: {Msg}", langCode, result?.Message ?? "");

            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Failed to retrieve products.",
                Path = "api/Products",
                Method = HttpMethods.Get,
                TraceId = HttpContext.TraceIdentifier
            };

            _logger.LogWarning("Returning error response with StatusCode: {StatusCode}, Message: {Message}", errorResponse.StatusCode, errorResponse.Message);

            return BadRequest(errorResponse);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetProductById(int id, int langId)
        {
            _logger.LogInformation("GetProductById controller called with Id: {Id}, LangId: {LangId}", id, langId);

            string baseUrl = BaseURL();
            string langCode = GetLangCode();

            _logger.LogDebug("BaseUrl: {BaseUrl}, LangCode: {LangCode}", baseUrl, langCode);

            var result = await _productService.GetProductById(id, langCode, baseUrl);

            _logger.LogDebug("Service call completed for Id: {Id}. IsSuccess: {IsSuccess}", id, result?.IsSuccess);

            if (result is not null && result.IsSuccess)
            {
                var productDetailsDto = result.Data;

                _logger.LogInformation("Successfully retrieved product details for Id: {Id}, LangCode: {LangCode}", id, langCode);

                return Ok(new ApiResponse<ProductDetailsDto>
                {
                    IsSuccess = true,
                    Message = "Product details retrieved successfully",
                    Data = productDetailsDto,
                });
            }

            _logger.LogWarning("Failed to retrieve product details for Id: {Id}, LangCode: {LangCode}. Message: {Msg}",
                id, langCode, result?.Message ?? "");

            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Failed to retrieve products details.",
                Path = "api/Products/{id}",
                Method = HttpMethods.Get,
                TraceId = HttpContext.TraceIdentifier
            };

            _logger.LogWarning("Returning error response for Id: {Id} with StatusCode: {StatusCode}, Message: {Message}",
                id, errorResponse.StatusCode, errorResponse.Message);

            return BadRequest(errorResponse);
        }

        [HttpGet]
        [Route("admin")]
        public async Task<IActionResult> GetAdminProducts([FromQuery] ProductFilterDto filterDto)
        {
            _logger.LogInformation("GetAdminProducts controller called with PageNumber: {PageNumber}, PageSize: {PageSize}",
                filterDto.PageNumber, filterDto.PageSize);

            var baseUrl = BaseURL();
            string langCode = GetLangCode();

            _logger.LogDebug("BaseUrl: {BaseUrl}, LangCode: {LangCode}", baseUrl, langCode);

            var result = await _productService.GetAllAdminProducts(filterDto, langCode, baseUrl);

            _logger.LogDebug("Service call completed. IsSuccess: {IsSuccess}", result?.IsSuccess);

            if (result is not null && result.IsSuccess)
            {
                var (items, totalCount) = result.Data;

                _logger.LogInformation("Successfully retrieved {ItemCount} admin products out of {TotalCount} total for LangCode: {LangCode}",
                    items?.Count() ?? 0, totalCount, langCode);

                return Ok(new
                {
                    items,
                    totalCount,
                    pageNumber = filterDto.PageNumber,
                    pageSize = filterDto.PageSize,
                    totalPages = (int)Math.Ceiling(result.Data.TotalCount / (double)filterDto.PageSize)
                });
            }

            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Failed to retrieve products for admin.",
                Path = "api/Products/admin",
                Method = HttpMethods.Get,
                TraceId = HttpContext.TraceIdentifier
            };

            _logger.LogWarning("Returning error response with StatusCode: {StatusCode}, Message: {Message}", errorResponse.StatusCode, errorResponse.Message);

            return BadRequest(errorResponse);
        }

        [HttpGet]
        [Route("admin/{id}")]
        public async Task<IActionResult> GetAdminProductById(int id)
        {
            string baseUrl = BaseURL();
            var result = await _productService.GetAdminProductbyId(id, baseUrl);

            if (result is not null && result.IsSuccess)
            {
                var productDto = result.Data;

                return Ok(new ApiResponse<ProductDetailsAdminDto>
                {
                    IsSuccess = true,
                    Data = productDto,
                });
            }

            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Failed to retrieve product details for admin.",
                Path = "api/Products/admin/{id}",
                Method = HttpMethods.Get,
                TraceId = HttpContext.TraceIdentifier
            };

            return BadRequest(errorResponse);
        }

        [HttpPost]
        [Route("admin/create")]
        public async Task<IActionResult> CreateProduct([FromForm] ProductDetailsAdminDto dto)
        {
            var baseUrl = BaseURL();

            var result = await _productService.CreateProduct(dto, baseUrl);

            if (result is not null && result.IsSuccess)
            {
                var productDto = result.Data;

                return Ok(new ApiResponse<ProductDetailsAdminDto>()
                {
                    IsSuccess = true,
                    Data = productDto,
                });
            }

            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Failed to create a new product.",
                Path = "api/Products/admin/create",
                Method = HttpMethods.Post,
                TraceId = HttpContext.TraceIdentifier
            };

            return BadRequest(errorResponse);
        }

        [HttpPut]
        [Route("admin/{id}/edit")]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] ProductDetailsAdminDto dto)
        {
            var baseUrl = BaseURL();
            var result = await _productService.UpdateProduct(id, dto, baseUrl);

            if (result is not null && result.IsSuccess)
            {
                var product = result.Data ?? new();

                return Ok(new ApiResponse<ProductDetailsAdminDto>()
                {
                    IsSuccess = true,
                    Data = product
                });
            }

            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Failed to update the product.",
                Path = "api/Products/admin/{id}/update",
                Method = HttpMethods.Put,
                TraceId = HttpContext.TraceIdentifier
            };

            return BadRequest(errorResponse);
        }

        [HttpDelete]
        [Route("admin/{id}/delete")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            _logger.LogInformation("HTTP DELETE request received for api/products/admin/{Id}/delete", id);

            var result = await _productService.DeleteProduct(id);

            if (result is not null && result.IsSuccess)
            {
                return Ok(new ApiResponse<int>
                {
                    IsSuccess = true,
                    Message = "Product deleted successfully."
                });
            }

            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Failed to create a new product.",
                Path = "api/Products/admin/{id}/delete",
                Method = HttpMethods.Delete,
                TraceId = HttpContext.TraceIdentifier
            };

            return BadRequest(errorResponse);
        }
    }
}
