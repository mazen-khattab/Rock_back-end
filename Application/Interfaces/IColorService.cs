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
        Task<ApiResponse<ColorDetailsAdminDto>> CreateColor(ColorDetailsAdminDto dto);
        Task<ApiResponse<ColorDetailsAdminDto>> UpdateColor(int id, ColorDetailsAdminDto dto);
        Task<ApiResponse<int>> DeleteColor(int id);
        Task<ApiResponse<(IEnumerable<ColorDto> Items, int TotalCount)>> GetAll();
        Task<ApiResponse<ColorDetailsDto>> GetById(int colorId);
        Task<ApiResponse<IEnumerable<ColorLookupDto>>> Lookup(string langCode);
    }
}
