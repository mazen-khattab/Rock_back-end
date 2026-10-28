using Core.Entities;
using Core.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Repositories
{
    public class Repo<T> : IRepo<T> where T : BaseEntity
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repo(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        /// <summary>
        /// Builds an IQueryable for the entity with optional filtering, tracking behavior,
        /// query filter control, and eager loading of related entities.
        /// The query is not executed until enumerated.
        /// </summary>
        public IQueryable<T> Query(
            Expression<Func<T, bool>>? filter = null,
            bool tracked = true,
            bool ignoreQueryFilters = false,
            params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = tracked ? _dbSet : _dbSet.AsNoTracking();

            if (ignoreQueryFilters)
                query = query.IgnoreQueryFilters();

            if (filter != null)
                query = query.Where(filter);

            if (includes?.Length > 0)
            {
                query = includes.Aggregate(query, (current, include) => current.Include(include));
            }

            return query;
        }

        /// <summary>
        /// Retrieves all entities matching the specified filter,
        /// with optional tracking behavior, query filter control,
        /// and eager loading of related entities.
        /// </summary>
        public async Task<IEnumerable<T>> GetAllAsync(
            Expression<Func<T, bool>>? filter = null,
            bool tracked = true,
            bool ignoreQueryFilters = false,
            params Expression<Func<T, object>>[] includes) => await Query(filter, tracked, ignoreQueryFilters, includes).ToListAsync();

        /// <summary>
        /// Retrieves a paged list of entities matching the specified filter,
        /// along with the total count of records before pagination.
        /// Intended for read-only, UI-driven scenarios.
        /// </summary>
        public async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
            Expression<Func<T, bool>>? filter = null,
            bool tracked = false, // Default to untracked for pagination
            bool ignoreQueryFilters = false,
            int pageNumber = 1,
            int pageSize = 10,
            params Expression<Func<T, object>>[] includes)
        {
            var query = Query(filter, tracked, ignoreQueryFilters, includes);

            int totalCount = await query.CountAsync();

            if (pageNumber > 0 && pageSize > 0)
            {
                query = query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
            }

            var items = await query.ToListAsync();

            return (items, totalCount);
        }

        /// <summary>
        /// Retrieves a single entity matching the specified predicate,
        /// with optional tracking behavior and eager loading of related entities.
        /// Returns null if no entity is found.
        /// </summary>
        public async Task<T?> GetAsync(
            Expression<Func<T, bool>>? predicate,
            bool tracked = true,
            params Expression<Func<T, object>>[] includes) => await Query(predicate, tracked, includes: includes).FirstOrDefaultAsync();

        /// <summary>
        /// Retrieves an entity by its primary key identifier,
        /// with optional eager loading of related entities.
        /// Returns null if the entity does not exist.
        /// </summary>
        public async Task<T?> GetByIdAsync(int id, params Expression<Func<T, object>>[] includes)
        {
            if (includes?.Length > 0)
            {
                return await Query(e => e.Id == id, includes: includes).FirstOrDefaultAsync();
            }

            return await _dbSet.FindAsync(id);
        }

        /// <summary>
        /// Returns the total number of entities matching the specified filter.
        /// </summary>
        public async Task<int> CountAsync(Expression<Func<T, bool>>? filter = null)
            => filter == null
                ? await _dbSet.CountAsync()
                : await _dbSet.CountAsync(filter);

        /// <summary>
        /// Determines whether any entity exists that matches the specified predicate.
        /// </summary>
        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate) => await _dbSet.AnyAsync(predicate);

        /// <summary>
        /// Adds a new entity to the Change Tracker.
        /// Note: This does NOT immediately persist the change. You must call SaveChangesAsync() on the UnitOfWork.
        /// </summary>
        public void Add(T entity) => _dbSet.Add(entity);

        /// <summary>
        /// Updates an existing entity in the Change Tracker.
        /// Note: This does NOT immediately persist the change. You must call SaveChangesAsync() on the UnitOfWork.
        /// </summary>
        public void Update(T entity) => _dbSet.Update(entity);

        /// <summary>
        /// Marks a collection of entities for deletion in the Change Tracker.
        /// Note: This does NOT immediately persist the change.
        /// </summary>
        public void DeleteRange(IEnumerable<T> entities) => _dbSet.RemoveRange(entities);

        /// <summary>
        /// Soft-delete behavior is handled by AppDbContext.SaveChangesAsync:
        /// When the entry State == Deleted and the entity implements ISoftDelete, 
        /// AppDbContext converts it to IsDeleted = true + Modified.
        /// So here we simply mark the entity for deletion; the context will convert it to a soft-delete upon saving.
        /// </summary>
        public void Delete(T entity) => _dbSet.Remove(entity);

        /// <summary>
        /// Physically deletes the row from the database using ExecuteDeleteAsync to bypass the change tracker path.
        /// This performs a server-side DELETE WHERE Id = {id} and will not be intercepted by AppDbContext's SaveChanges logic.
        /// </summary>
        public async Task HardDeleteAsync(T entity)
        {
            // Ensure we delete by key to avoid tracking-based deletion that AppDbContext would intercept.
            await _dbSet.Where(e => e.Id == entity.Id).ExecuteDeleteAsync();
        }
    }
}
