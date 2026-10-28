using Application.DTOs;
using Application.Responses;
using Core.Entities;
using Core.Interfaces;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ICategoryService : IServices<Category>
    {
        Task<ApiResponse<(IEnumerable<CategoryDto> Items, int TotalCount)>> GetAll(string langCode);
        Task<(IEnumerable<Category>, int)> FetchAllCategoriesAsync(int langId);
        Task<ApiResponse<CategoryDetailsDto>> GetById(int categoryId, string langCode);
        Task<Category?> FetchCategoryByIdAsync(int categoryId, int langId);
        Task<ApiResponse<IEnumerable<CategoryLookupDto>>> Lookup(string langCode);
    }
}