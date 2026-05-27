using Application.DTOs;
using Application.Responses;
using Core.Entities;
using Core.Interfaces;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ISizeService : IServices<Size>
    {
        Task<ApiResponse<(IEnumerable<Size> Items, int TotalCount)>> GetAll();
        Task<ApiResponse<Size>> GetById(int sizeId);
        Task<ApiResponse<IEnumerable<Size>>> Lookup();
    }
}
