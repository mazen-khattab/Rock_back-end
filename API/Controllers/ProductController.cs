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
        private readonly IMapper _mapper;

        public ProductController(IProductService productService, IMapper mapper)
        {
            _productService = productService;
            _mapper = mapper;
        }

        [HttpGet]
        [Route("Products/{langId}")]
        public async Task<IActionResult> GetAllProducts(int langId, [FromQuery] ProductFilterDto filterDto)
        {
            var products = await _productService.GetFilteredProductsAsync(langId, filterDto);
            var productDto = _mapper.MapToDtoList(products.Data.Items, langId);

            return Ok(new
            {
                items = productDto,
                totalCount = products.Data.TotalCount,
                pageNumber = filterDto.PageNumber,
                pageSize = filterDto.PageSize,
                totalPages = (int)Math.Ceiling(products.Data.TotalCount / (double)filterDto.PageSize)
            });
        }

        [HttpGet]
        [Route("{id}/{langId}")]
        public async Task<IActionResult> GetProductById(int id, int langId)
        {
            var product = await _productService.GetProductById(id, langId);

            if (!product.IsSucess)
            {
                return NotFound(product.Message);
            }

            var productDto = _mapper.MapToDto(product.Data, langId);

            return Ok(productDto);
        }
    }
}
