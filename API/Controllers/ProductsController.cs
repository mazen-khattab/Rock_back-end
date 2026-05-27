using Application.DTOs;
using Application.Interfaces;
using Application.Mapping;
using Application.Responses;
using Core.Entities;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet]
        [Route("{langId}")]
        public async Task<IActionResult> GetAllProducts(int langId, [FromQuery] ProductFilterDto filterDto)
        {
            var result = await _productService.GetFilteredProductsAsync(langId, filterDto);

            if (!result.IsSuccess)
            {
                return BadRequest(new { error = result.Message });
            }

            var (products, totalCount) = result.Data;
            var productDto = products?.ToDtoList(langId) ?? new List<AllProductsDto>();

            return Ok(new
            {
                items = productDto,
                totalCount = totalCount,
                pageNumber = filterDto.PageNumber,
                pageSize = filterDto.PageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)filterDto.PageSize)
            });
        }

        [HttpGet]
        [Route("{id}/{langId}")]
        public async Task<IActionResult> GetProductById(int id, int langId)
        {
            var product = await _productService.GetProductById(id, langId);

            if (!product.IsSuccess)
            {
                return NotFound(product.Message);
            }

            var productDto = product.Data?.ToDetailsDto(langId) ?? new ProductDetailsDto();

            return Ok(new ApiResponse<ProductDetailsDto>
            {
                IsSuccess = true,
                Message = "Product retrieved successfully",
                Data = productDto,
            });
        }

        [HttpGet]
        [Route("admin/{langId}")]
        public async Task<IActionResult> GetAdminProducts(int langId, [FromQuery] ProductFilterDto filterDto)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

            _logger.LogInformation("Base URL for product images: {BaseUrl}", baseUrl);

            var products = await _productService.GetFilteredProductsAsync(langId, filterDto);
            var productDtos = products.Data.Items?.ToDtoAdminList(langId, baseUrl) ?? new List<AllProductsAdminDto>();

            return Ok(new
            {
                items = productDtos,
                totalCount = products.Data.TotalCount,
                pageNumber = filterDto.PageNumber,
                pageSize = filterDto.PageSize,
                totalPages = (int)Math.Ceiling(products.Data.TotalCount / (double)filterDto.PageSize)
            });
        }

        [HttpPost]
        [Route("admin/Products")]
        public async Task<IActionResult> CreateProduct([FromForm] ProductDetailsAdminDto dto)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            _logger.LogInformation("Creating product with name: {NameEn}", dto.NameEn);

            var response = await _productService.CreateProduct(dto);

            if (!response.IsSuccess)
            {
                return BadRequest(new { error = response.Message });
            }

            Product product = response.Data ?? new();

            return Ok(new ApiResponse<AllProductsAdminDto>()
            {
                IsSuccess = true,
                Data = product.ToAdminDto(2, baseUrl)
            });
        }
    }
}
