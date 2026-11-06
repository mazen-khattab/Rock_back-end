using API.Extensions;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
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
        public async Task<IActionResult> GetAllProducts([FromQuery] ProductFilterDto filterDto)
        {
            _logger.LogInformation("Product list requested, page: {PageNumber}, size: {PageSize}", filterDto.PageNumber, filterDto.PageSize);
            var result = await _productService.GetAllProducts(filterDto, Request.GetLanguageCode(), Request.GetBaseUrl());
            return this.ToPagedApiResult(result, filterDto.PageNumber, filterDto.PageSize);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(int id, int langId)
        {
            _logger.LogInformation("Product details requested for ID: {ProductId}", id);
            return this.ToApiResult(await _productService.GetProductById(id, Request.GetLanguageCode(), Request.GetBaseUrl()));
        }

        [HttpGet("admin")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> GetAdminProducts([FromQuery] ProductFilterDto filterDto)
        {
            _logger.LogInformation("Admin product list requested, page: {PageNumber}, size: {PageSize}", filterDto.PageNumber, filterDto.PageSize);
            var result = await _productService.GetAllAdminProducts(filterDto, Request.GetLanguageCode(), Request.GetBaseUrl());
            return this.ToPagedApiResult(result, filterDto.PageNumber, filterDto.PageSize);
        }

        [HttpGet("admin/{id}")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> GetAdminProductById(int id)
        {
            _logger.LogInformation("Admin product details requested for ID: {ProductId}", id);
            return this.ToApiResult(await _productService.GetAdminProductbyId(id, Request.GetBaseUrl()));
        }

        [HttpPost("admin/create")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> CreateProduct([FromForm] ProductDetailsAdminDto dto)
        {
            _logger.LogInformation("Product creation requested");
            return this.ToApiResult(await _productService.CreateProduct(dto, Request.GetBaseUrl()));
        }

        [HttpPut("admin/{id}/edit")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] ProductDetailsAdminDto dto)
        {
            _logger.LogInformation("Product update requested for ID: {ProductId}", id);
            return this.ToApiResult(await _productService.UpdateProduct(id, dto, Request.GetBaseUrl()));
        }

        [HttpDelete("admin/{id}/delete")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            _logger.LogInformation("Product deletion requested for ID: {ProductId}", id);
            return this.ToApiResult(await _productService.DeleteProduct(id));
        }
    }
}
