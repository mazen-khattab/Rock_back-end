using System.Linq.Expressions;
using Core.Entities;

namespace Core.Interfaces
{
    public interface IAccountRepository
    {
        IQueryable<User> Query(bool tracked = false);
        Task<bool> HasOrdersAsync(int id);
        Task<int> DeleteWithoutOrdersAsync(int id, Expression<Func<User, bool>> accountScope);
        Task RevokeRefreshTokensAsync(int id);
        Task RestoreRoleAssignmentsAsync(int id, IReadOnlyCollection<string> normalizedRoleNames);
    }
}
