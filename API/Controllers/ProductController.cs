using Application.DTOs;
using Application.Interfaces;
using Application.Mapping;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        [Route("Products/{langId}")]
        public async Task<IActionResult> GetAllProducts(int langId, [FromQuery] ProductFilterDto filterDto)
        {
            var productDto = await _productService.GetFilteredProductsAsync(langId, filterDto);

            return Ok(new
            {
                items = productDto.Data.Items,
                totalCount = productDto.Data.TotalCount,
                pageNumber = filterDto.PageNumber,
                pageSize = filterDto.PageSize,
                totalPages = (int)Math.Ceiling(productDto.Data.TotalCount / (double)filterDto.PageSize)
            });
        }

        [HttpGet]
        [Route("{id}/{langId}")]
        public async Task<IActionResult> GetProductById(int id, int langId)
        {
            var product = await _productService.GetProductById(id, langId);

            if (!product.Success)
            {
                return NotFound(product.Message);
            }

            return Ok(product.Data);
        }
    }
}
