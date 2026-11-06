using System.ComponentModel.DataAnnotations;
using API.Extensions;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
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

        /// <summary>
        /// Creates a category with Arabic and English translations for the admin panel.
        /// </summary>
        [HttpPost("admin/create")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryDetailsAdminDto dto)
        {
            _logger.LogInformation("Category creation requested");
            var result = await _categoryService.CreateCategory(dto);
            return result.IsSuccess
                ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result)
                : this.ToApiResult(result);
        }

        [HttpPut("admin/{id:int}/edit")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> UpdateCategory([Range(1, int.MaxValue)] int id, [FromBody] CategoryDetailsAdminDto dto)
        {
            _logger.LogInformation("Category update requested for ID: {CategoryId}", id);
            return this.ToApiResult(await _categoryService.UpdateCategory(id, dto));
        }

        /// <summary>
        /// Permanently deletes a category only when no products reference it.
        /// </summary>
        [HttpDelete("admin/{id:int}/delete")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> DeleteCategory([Range(1, int.MaxValue)] int id)
        {
            _logger.LogInformation("Category deletion requested for ID: {CategoryId}", id);
            return this.ToApiResult(await _categoryService.DeleteCategory(id));
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("Category list requested with Arabic and English names");
            return this.ToPagedApiResult(await _categoryService.GetAll());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            _logger.LogInformation("Category details requested for ID: {CategoryId}", id);
            return this.ToApiResult(await _categoryService.GetById(id));
        }

        [HttpGet("lookup")]
        public async Task<IActionResult> GetLookup()
        {
            _logger.LogInformation("Category lookup requested");
            return this.ToApiResult(await _categoryService.Lookup(Request.GetLanguageCode()));
        }
    }
}
