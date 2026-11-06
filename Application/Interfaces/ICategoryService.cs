using Application.DTOs;
using Application.Responses;
using Core.Entities;
using Core.Interfaces;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ICategoryService : IServices<Category>
    {
        Task<ApiResponse<CategoryDetailsAdminDto>> CreateCategory(CategoryDetailsAdminDto dto);
        Task<ApiResponse<CategoryDetailsAdminDto>> UpdateCategory(int id, CategoryDetailsAdminDto dto);
        Task<ApiResponse<int>> DeleteCategory(int id);
        Task<ApiResponse<(IEnumerable<CategoryDto> Items, int TotalCount)>> GetAll();
        Task<ApiResponse<CategoryDetailsDto>> GetById(int categoryId);
        Task<ApiResponse<IEnumerable<CategoryLookupDto>>> Lookup(string langCode);
    }
}
