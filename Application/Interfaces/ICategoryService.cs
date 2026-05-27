using Application.DTOs;
using Application.Responses;
using Core.Entities;
using Core.Interfaces;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ICategoryService : IServices<Category>
    {
        Task<ApiResponse<(IEnumerable<Category> Items, int TotalCount)>> GetAll(int langId);
        Task<ApiResponse<Category>> GetById(int categoryId, int langId);
        Task<ApiResponse<IEnumerable<Category>>> Lookup(int langId);
    }
}