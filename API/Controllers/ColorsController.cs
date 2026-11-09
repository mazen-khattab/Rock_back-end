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
    public class ColorsController : ControllerBase
    {
        private readonly IColorService _colorService;
        private readonly ILogger<ColorsController> _logger;

        public ColorsController(IColorService colorService, ILogger<ColorsController> logger)
        {
            _colorService = colorService;
            _logger = logger;
        }

        /// <summary>
        /// Creates a color with its hex code and Arabic and English names.
        /// </summary>
        [HttpPost("admin/create")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> CreateColor([FromBody] ColorDetailsAdminDto dto)
        {
            _logger.LogInformation("Color creation requested");
            var result = await _colorService.CreateColor(dto);
            return result.IsSuccess
                ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result)
                : this.ToApiResult(result);
        }

        [HttpPut("admin/{id:int}/edit")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> UpdateColor([Range(1, int.MaxValue)] int id, [FromBody] ColorDetailsAdminDto dto)
        {
            _logger.LogInformation("Color update requested for ID: {ColorId}", id);
            return this.ToApiResult(await _colorService.UpdateColor(id, dto));
        }

        /// <summary>
        /// Permanently deletes a color only when no variants reference it.
        /// </summary>
        [HttpDelete("admin/{id:int}/delete")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> DeleteColor([Range(1, int.MaxValue)] int id)
        {
            _logger.LogInformation("Color deletion requested for ID: {ColorId}", id);
            return this.ToApiResult(await _colorService.DeleteColor(id));
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("Color list requested with Arabic and English names");
            return this.ToPagedApiResult(await _colorService.GetAll());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            _logger.LogInformation("Color details requested for ID: {ColorId}", id);
            return this.ToApiResult(await _colorService.GetById(id));
        }

        [HttpGet("lookup")]
        public async Task<IActionResult> GetLookup()
        {
            _logger.LogInformation("Color lookup requested");
            return this.ToApiResult(await _colorService.Lookup(Request.GetLanguageCode()));
        }
    }
}
