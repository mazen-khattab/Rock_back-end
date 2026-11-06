using System.Linq.Expressions;
using Core.Entities;
using Core.ExceptionsTypes;
using Core.Interfaces;
using Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly AppDbContext _context;

        public AccountRepository(AppDbContext context) => _context = context;

        public IQueryable<User> Query(bool tracked = false) => tracked ? _context.Users : _context.Users.AsNoTracking();

        public Task<bool> HasOrdersAsync(int id) =>
            _context.Orders.IgnoreQueryFilters().AnyAsync(o => o.UserId == id);

        public async Task<int> DeleteWithoutOrdersAsync(int id, Expression<Func<User, bool>> accountScope)
        {
            try
            {
                // Include deleted orders in the same DELETE condition to protect order history.
                return await _context.Users.IgnoreQueryFilters()
                    .Where(u => u.Id == id && !u.IsDeleted && !u.Orders.Any())
                    .Where(accountScope).ExecuteDeleteAsync();
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                // Other historical references and orders added concurrently remain protected by foreign keys.
                throw new EntityInUseException("The account is referenced by historical data.", ex);
            }
        }

        public async Task RevokeRefreshTokensAsync(int id) =>
            await _context.RefreshTokens.Where(t => t.UserId == id).ExecuteDeleteAsync();

        public async Task RestoreRoleAssignmentsAsync(int id, IReadOnlyCollection<string> normalizedRoleNames)
        {
            // Identity role removals are soft deleted in this project; reuse the composite-key row.
            await _context.UserRoles.IgnoreQueryFilters()
                .Where(ur => ur.UserId == id && ur.IsDeleted && !ur.Role.IsDeleted
                    && normalizedRoleNames.Contains(ur.Role.NormalizedName!))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(ur => ur.IsDeleted, false)
                    .SetProperty(ur => ur.DeletedAt, (DateTime?)null)
                    .SetProperty(ur => ur.UpdatedAt, DateTime.Now));
        }
    }
}
