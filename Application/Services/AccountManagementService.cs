using System.Data;
using System.Linq.Expressions;
using Application.DTOs;
using Application.Interfaces;
using Application.Mapping;
using Application.Responses;
using Core.Entities;
using Core.Enum;
using Core.ExceptionsTypes;
using Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class AccountManagementService : IAccountManagementService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IAccountRepository _accounts;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AccountManagementService> _logger;

        public AccountManagementService(UserManager<User> userManager, RoleManager<Role> roleManager,
            IAccountRepository accounts, IUnitOfWork unitOfWork, ILogger<AccountManagementService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _accounts = accounts;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public Task<ApiResponse<PagedResultDto<ManagedAccountDto>>> ListStaff(AccountFilterDto filter) => ListAccounts(filter, true);
        public Task<ApiResponse<PagedResultDto<ManagedAccountDto>>> ListCustomers(AccountFilterDto filter) => ListAccounts(filter, false);
        public Task<ApiResponse<ManagedAccountDto>> GetStaff(int id) => GetAccount(id, true);
        public Task<ApiResponse<ManagedAccountDto>> GetCustomer(int id) => GetAccount(id, false);

        public Task<ApiResponse<ManagedAccountDto>> CreateStaff(CreateStaffDto dto, int actorId)
        {
            var roles = NormalizeStaffRoles(dto.Roles);
            return roles == null
                ? Task.FromResult(Failure<ManagedAccountDto>(ErrorType.ValidationError, "Choose one or both of the Admin and Owner roles."))
                : WriteAccount(true, actorId, () => CreateAccount(dto, roles, true));
        }

        public Task<ApiResponse<ManagedAccountDto>> CreateCustomer(CreateCustomerDto dto, int actorId) =>
            WriteAccount(false, actorId, () => CreateAccount(dto, new[] { "User" }, false));

        public Task<ApiResponse<ManagedAccountDto>> UpdateStaff(int id, UpdateStaffDto dto, int actorId)
        {
            var roles = dto.Roles == null ? null : NormalizeStaffRoles(dto.Roles);
            if (dto.Roles != null && roles == null)
                return Task.FromResult(Failure<ManagedAccountDto>(ErrorType.ValidationError, "Choose one or both of the Admin and Owner roles."));
            return WriteAccount(true, actorId, () => UpdateAccount(id, dto, roles, true, actorId));
        }

        public Task<ApiResponse<ManagedAccountDto>> UpdateCustomer(int id, AccountProfileRequestDto dto, int actorId) =>
            WriteAccount(false, actorId, () => UpdateAccount(id, dto, null, false, actorId));

        public Task<ApiResponse<int>> DeleteStaff(int id, int actorId) =>
            WriteAccount(true, actorId, () => DeleteAccount(id, true, actorId));
        public Task<ApiResponse<int>> DeleteCustomer(int id, int actorId) =>
            WriteAccount(false, actorId, () => DeleteAccount(id, false, actorId));

        private static Expression<Func<User, bool>> AccountScope(bool staff)
        {
            if (staff)
                return u => u.UserRoles.Any(ur => !ur.IsDeleted && !ur.Role.IsDeleted
                    && (ur.Role.NormalizedName == "ADMIN" || ur.Role.NormalizedName == "OWNER"));

            // Seeded owners also have User: a privileged account must never become a customer target.
            return u => u.UserRoles.Any(ur => !ur.IsDeleted && !ur.Role.IsDeleted && ur.Role.NormalizedName == "USER")
                && !u.UserRoles.Any(ur => !ur.IsDeleted && !ur.Role.IsDeleted
                    && (ur.Role.NormalizedName == "ADMIN" || ur.Role.NormalizedName == "OWNER"));
        }

        private IQueryable<User> ScopedAccounts(bool staff, bool tracked = false) =>
            _accounts.Query(tracked).IgnoreQueryFilters().Where(u => !u.IsDeleted).Where(AccountScope(staff));

        private async Task<ApiResponse<PagedResultDto<ManagedAccountDto>>> ListAccounts(AccountFilterDto filter, bool staff)
        {
            _logger.LogInformation("Listing {AccountType} accounts, page: {PageNumber}, size: {PageSize}",
                staff ? "staff" : "customer", filter.PageNumber, filter.PageSize);
            long skip = ((long)filter.PageNumber - 1) * filter.PageSize;
            if (filter.PageNumber < 1 || filter.PageSize < 1 || filter.PageSize > 100 || skip > int.MaxValue)
                return Failure<PagedResultDto<ManagedAccountDto>>(ErrorType.ValidationError, "Invalid page number or page size.");

            var query = ScopedAccounts(staff);
            string? search = filter.Search?.Trim();
            if (!string.IsNullOrEmpty(search))
                query = query.Where(u => u.Fname.Contains(search) || u.Lname.Contains(search)
                    || (u.Email != null && u.Email.Contains(search))
                    || (u.PhoneNumber != null && u.PhoneNumber.Contains(search)));

            int count = await query.CountAsync();
            var items = await query.OrderByDescending(u => u.CreatedAt).ThenByDescending(u => u.Id)
                .Skip((int)skip).Take(filter.PageSize).ProjectToManagedAccount().ToListAsync();
            return new()
            {
                IsSuccess = true,
                Data = new() { Items = items, TotalCount = count, PageNumber = filter.PageNumber, PageSize = filter.PageSize },
                Message = "Accounts retrieved successfully."
            };
        }

        private async Task<ApiResponse<ManagedAccountDto>> GetAccount(int id, bool staff)
        {
            _logger.LogInformation("Retrieving {AccountType} account ID: {AccountId}", staff ? "staff" : "customer", id);
            var account = await ScopedAccounts(staff).Where(u => u.Id == id).ProjectToManagedAccount().SingleOrDefaultAsync();
            return account == null ? Failure<ManagedAccountDto>(ErrorType.NotFound, "Account not found.")
                : new() { IsSuccess = true, Data = account, Message = "Account retrieved successfully." };
        }

        private async Task<ApiResponse<ManagedAccountDto>> CreateAccount(CreateCustomerDto dto, string[] roles, bool staff)
        {
            if (dto.Password != dto.ConfirmPassword)
                return Failure<ManagedAccountDto>(ErrorType.ValidationError, "Passwords do not match.");
            if (!await RolesExist(roles))
                return Failure<ManagedAccountDto>(ErrorType.ValidationError, "The requested account roles are not configured.");
            if (await DuplicateContact(dto, 0))
                return Failure<ManagedAccountDto>(ErrorType.Conflict, "The email or phone number is already in use.");

            var user = new User();
            user.ApplyManagedProfile(dto);
            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return IdentityFailure<ManagedAccountDto>(result);

            result = await _userManager.AddToRolesAsync(user, roles);
            if (!result.Succeeded)
                return IdentityFailure<ManagedAccountDto>(result);

            _logger.LogInformation("Created {AccountType} account ID: {AccountId}", staff ? "staff" : "customer", user.Id);
            return await GetAccount(user.Id, staff);
        }

        private async Task<ApiResponse<ManagedAccountDto>> UpdateAccount(int id, AccountProfileRequestDto dto,
            string[]? requestedRoles, bool staff, int actorId)
        {
            var user = await ScopedAccounts(staff, tracked: true).SingleOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return Failure<ManagedAccountDto>(ErrorType.NotFound, "Account not found.");
            if (await DuplicateContact(dto, id))
                return Failure<ManagedAccountDto>(ErrorType.Conflict, "The email or phone number is already in use.");

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (requestedRoles != null)
            {
                if (!await RolesExist(requestedRoles))
                    return Failure<ManagedAccountDto>(ErrorType.ValidationError, "The requested account roles are not configured.");
                if (currentRoles.Contains("Owner") && !requestedRoles.Contains("Owner"))
                {
                    if (id == actorId || await OwnerCount() <= 1)
                        return Failure<ManagedAccountDto>(ErrorType.Conflict, "You cannot remove your own Owner role or remove the last Owner.");
                }
            }

            user.ApplyManagedProfile(dto);
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return IdentityFailure<ManagedAccountDto>(result);

            if (requestedRoles != null)
            {
                var staffRoles = currentRoles.Where(r => r == "Admin" || r == "Owner").ToArray();
                if (!staffRoles.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(requestedRoles))
                {
                    result = await _userManager.RemoveFromRolesAsync(user, staffRoles.Except(requestedRoles));
                    if (!result.Succeeded)
                        return IdentityFailure<ManagedAccountDto>(result);

                    await _accounts.RestoreRoleAssignmentsAsync(id, requestedRoles.Select(r => r.ToUpperInvariant()).ToArray());
                    var existingRoles = await _userManager.GetRolesAsync(user);
                    result = await _userManager.AddToRolesAsync(user, requestedRoles.Except(existingRoles));
                    if (!result.Succeeded)
                        return IdentityFailure<ManagedAccountDto>(result);
                    result = await _userManager.UpdateSecurityStampAsync(user);
                    if (!result.Succeeded)
                        return IdentityFailure<ManagedAccountDto>(result);
                    await _accounts.RevokeRefreshTokensAsync(id);
                }
            }

            _logger.LogInformation("Updated account ID: {AccountId}", id);
            return await GetAccount(id, staff);
        }

        private async Task<ApiResponse<int>> DeleteAccount(int id, bool staff, int actorId)
        {
            var user = await ScopedAccounts(staff).Where(u => u.Id == id).Select(u => new { u.Id }).SingleOrDefaultAsync();
            if (user == null)
                return Failure<int>(ErrorType.NotFound, "Account not found.");
            if (await _accounts.HasOrdersAsync(id))
                return Failure<int>(ErrorType.Conflict, "Accounts with orders cannot be deleted, including accounts with deleted orders.");
            if (id == actorId)
                return Failure<int>(ErrorType.Conflict, "You cannot delete your own account.");

            try
            {
                // Hard deletion also removes Identity claims, role links and refresh tokens through cascades.
                int deleted = await _accounts.DeleteWithoutOrdersAsync(id, AccountScope(staff));
                if (deleted == 0)
                    return Failure<int>(ErrorType.Conflict, "The account changed or received an order. Refresh and try again.");
            }
            catch (EntityInUseException)
            {
                return Failure<int>(ErrorType.Conflict, "The account cannot be deleted because it is referenced by historical data.");
            }

            _logger.LogInformation("Permanently deleted account ID: {AccountId}", id);
            return new() { IsSuccess = true, Data = id, Message = "Account deleted successfully." };
        }

        private async Task<ApiResponse<T>> WriteAccount<T>(bool staff, int actorId, Func<Task<ApiResponse<T>>> operation)
        {
            _logger.LogInformation("Account management write requested by actor ID: {ActorId}, account type: {AccountType}",
                actorId, staff ? "staff" : "customer");
            // Serialize role and order checks with account changes, including concurrent owner changes.
            await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                string requiredRole = staff ? "OWNER" : "ADMIN";
                bool allowed = await _accounts.Query().Where(u => u.Id == actorId).AnyAsync(u => u.UserRoles.Any(ur =>
                    !ur.IsDeleted && !ur.Role.IsDeleted && ur.Role.NormalizedName == requiredRole));
                var response = allowed ? await operation() : Failure<T>(ErrorType.Forbidden, "You do not have permission to manage these accounts.");
                if (response.IsSuccess)
                    await _unitOfWork.CommitAsync();
                else
                    await _unitOfWork.RollbackAsync();
                return response;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogWarning(ex, "Account changed during management write by actor ID: {ActorId}", actorId);
                return Failure<T>(ErrorType.ConcurrencyConflict, "The account changed. Refresh and try again.");
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        private Task<int> OwnerCount() => ScopedAccounts(true).CountAsync(u => u.UserRoles.Any(ur =>
            !ur.IsDeleted && !ur.Role.IsDeleted && ur.Role.NormalizedName == "OWNER"));

        private Task<bool> DuplicateContact(AccountProfileRequestDto dto, int exceptId)
        {
            string normalizedEmail = _userManager.NormalizeEmail(dto.Email.Trim());
            string phone = dto.Phone.Trim();
            return _accounts.Query().IgnoreQueryFilters().AnyAsync(u => u.Id != exceptId
                && (u.NormalizedEmail == normalizedEmail || u.NormalizedUserName == normalizedEmail || u.PhoneNumber == phone));
        }

        private async Task<bool> RolesExist(IEnumerable<string> roles)
        {
            foreach (string role in roles)
                if (!await _roleManager.RoleExistsAsync(role))
                    return false;
            return true;
        }

        private static string[]? NormalizeStaffRoles(List<string>? roles)
        {
            if (roles == null || roles.Count < 1 || roles.Count > 2)
                return null;
            var normalized = roles.Select(r => r?.Trim()).Select(r =>
                string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase) ? "Admin" :
                string.Equals(r, "Owner", StringComparison.OrdinalIgnoreCase) ? "Owner" : null).ToArray();
            return normalized.Any(r => r == null) ? null : normalized.Select(r => r!).Distinct().ToArray();
        }

        private ApiResponse<T> IdentityFailure<T>(IdentityResult result)
        {
            _logger.LogWarning("Account identity operation failed with codes: {Codes}", string.Join(", ", result.Errors.Select(e => e.Code)));
            bool duplicate = result.Errors.Any(e => e.Code == "DuplicateEmail" || e.Code == "DuplicateUserName");
            return Failure<T>(duplicate ? ErrorType.Conflict : ErrorType.ValidationError,
                string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        private ApiResponse<T> Failure<T>(ErrorType errorType, string message)
        {
            _logger.LogWarning("Account management failed with {ErrorType}: {Message}", errorType, message);
            return new() { IsSuccess = false, ErrorType = errorType, Message = message };
        }
    }
}
