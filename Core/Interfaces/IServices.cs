using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Core.Interfaces
{
    public interface IServices<T> where T : class
    {
        /// <summary>
        /// Builds an IQueryable for the entity with optional filtering, tracking behavior,
        /// query filter control, and eager loading of related entities.
        /// The query is not executed until enumerated.
        /// </summary>
        IQueryable<T> Query(
            Expression<Func<T, bool>>? filter = null,
            bool tracked = true,
            bool ignoreQueryFilters = false,
            params Expression<Func<T, object>>[] includes);


        /// <summary>
        /// Retrieves a single entity matching the specified filter,
        /// with optional tracking, query filter control, and eager loading.
        /// Returns null if no entity is found.
        /// </summary>
        Task<T?> GetAsync(
            Expression<Func<T, bool>>? filter = null,
            bool tracked = true,
            params Expression<Func<T, object>>[] includes);


        /// <summary>
        /// Retrieves an entity by its primary key identifier,
        /// with optional eager loading of related entities.
        /// Returns null if the entity does not exist.
        /// </summary>
        Task<T?> GetByIdAsync(int id, params Expression<Func<T, object>>[] includes);


        /// <summary>
        /// Retrieves all entities matching the specified filter,
        /// with optional tracking behavior and eager loading of related entities.
        /// </summary>
        Task<IEnumerable<T>> GetAllAsync(
            Expression<Func<T, bool>>? filter = null,
            bool ignoreQueryFilters = false,
            bool tracked = true,
            params Expression<Func<T, object>>[] includes);


        /// <summary>
        /// Retrieves a paged list of entities matching the specified filter,
        /// along with the total count of records before pagination.
        /// Intended for read-only, UI-driven scenarios.
        /// </summary>
        Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
            Expression<Func<T, bool>>? filter = null,
            bool tracked = false, // Default to untracked for pagination
            bool ignoreQueryFilters = false,
            int pageNumber = 1,
            int pageSize = 10,
            params Expression<Func<T, object>>[] includes);


        /// <summary>
        /// Determines whether any entity exists that matches the specified predicate.
        /// </summary>
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);


        /// <summary>
        /// Adds a new entity to the data store.
        /// The change is not persisted until SaveChangesAsync is called.
        /// </summary>
        Task AddAsync(T entity);


        /// <summary>
        /// Updates an existing entity in the data store.
        /// The change is not persisted until SaveChangesAsync is called.
        /// </summary>
        Task UpdateAsync(T entity);


        /// <summary>
        /// Soft-delete if the entity implements ISoftDelete, otherwise physically delete.
        /// </summary>
        Task DeleteAsync(T entity);


        /// <summary>
        /// Physically delete regardless of ISoftDelete.
        /// </summary>
        Task HardDeleteAsync(T entity);
        Task<int> SaveChangesAsync();
    }
}
