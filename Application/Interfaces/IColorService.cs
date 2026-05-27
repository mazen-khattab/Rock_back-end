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
        Task<ApiResponse<(IEnumerable<Color> Items, int TotalCount)>> GetAll(int langId);
        Task<ApiResponse<Color>> GetById(int categoryId, int langId);
        Task<ApiResponse<IEnumerable<Color>>> Lookup(int langId);
    }
}
