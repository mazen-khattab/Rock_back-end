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
    public class SizesController : ControllerBase
    {
        private readonly ISizeService _sizeService;
        private readonly ILogger<SizesController> _logger;

        public SizesController(ISizeService sizeService, ILogger<SizesController> logger)
        {
            _sizeService = sizeService;
            _logger = logger;
        }

        [HttpPost("admin/create")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> CreateSize([FromBody] SizeDetailsDto dto)
        {
            _logger.LogInformation("Size creation requested");
            var result = await _sizeService.CreateSize(dto);
            return result.IsSuccess
                ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result)
                : this.ToApiResult(result);
        }

        [HttpPut("admin/{id:int}/edit")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> UpdateSize([Range(1, int.MaxValue)] int id, [FromBody] SizeDetailsDto dto)
        {
            _logger.LogInformation("Size update requested for ID: {SizeId}", id);
            return this.ToApiResult(await _sizeService.UpdateSize(id, dto));
        }

        /// <summary>
        /// Permanently deletes a size only when no variants reference it.
        /// </summary>
        [HttpDelete("admin/{id:int}/delete")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> DeleteSize([Range(1, int.MaxValue)] int id)
        {
            _logger.LogInformation("Size deletion requested for ID: {SizeId}", id);
            return this.ToApiResult(await _sizeService.DeleteSize(id));
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("Size list requested");
            return this.ToPagedApiResult(await _sizeService.GetAll());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            _logger.LogInformation("Size details requested for ID: {SizeId}", id);
            return this.ToApiResult(await _sizeService.GetById(id));
        }

        [HttpGet("lookup")]
        public async Task<IActionResult> GetLookup()
        {
            _logger.LogInformation("Size lookup requested");
            return this.ToApiResult(await _sizeService.Lookup());
        }
    }
}
