using Application.DTOs;
using Application.Responses;
using Core.Entities;
using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IColorService : IServices<Color>
    {
        Task<ApiResponse<(IEnumerable<ColorDto> Items, int TotalCount)>> GetAll(string langCode);
        Task<ApiResponse<ColorDetailsDto>> GetById(int colorId, string langCode);
        Task<ApiResponse<IEnumerable<ColorLookupDto>>> Lookup(string langCode);
    }
}
