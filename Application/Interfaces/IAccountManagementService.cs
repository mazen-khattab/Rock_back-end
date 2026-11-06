using Application.DTOs;
using Application.Responses;

namespace Application.Interfaces
{
    public interface IAccountManagementService
    {
        Task<ApiResponse<PagedResultDto<ManagedAccountDto>>> ListStaff(AccountFilterDto filter);
        Task<ApiResponse<PagedResultDto<ManagedAccountDto>>> ListCustomers(AccountFilterDto filter);
        Task<ApiResponse<ManagedAccountDto>> GetStaff(int id);
        Task<ApiResponse<ManagedAccountDto>> GetCustomer(int id);
        Task<ApiResponse<ManagedAccountDto>> CreateStaff(CreateStaffDto dto, int actorId);
        Task<ApiResponse<ManagedAccountDto>> CreateCustomer(CreateCustomerDto dto, int actorId);
        Task<ApiResponse<ManagedAccountDto>> UpdateStaff(int id, UpdateStaffDto dto, int actorId);
        Task<ApiResponse<ManagedAccountDto>> UpdateCustomer(int id, AccountProfileRequestDto dto, int actorId);
        Task<ApiResponse<int>> DeleteStaff(int id, int actorId);
        Task<ApiResponse<int>> DeleteCustomer(int id, int actorId);
    }
}
