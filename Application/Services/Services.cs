using Core.Entities;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Application.Services
{
    public class Services<T> : IServices<T> where T : BaseEntity
    {
        protected readonly IUnitOfWork _unitOfWork;
        protected readonly IRepo<T> _repo;
        protected readonly ILogger<Services<T>> _logger;

        public Services(IUnitOfWork unitOfWork, IRepo<T> repo, ILogger<Services<T>> logger) 
            => (_unitOfWork, _repo, _logger) = (unitOfWork, repo, logger);

        /// <summary>
        /// Builds and returns a queryable sequence of entities with optional filtering,
        /// tracking behavior, global query filter handling, and eager-loading includes.
        /// </summary>
        /// <param name="filter">Optional expression to filter entities.</param>
        /// <param name="tracked">Whether the entities should be tracked by the DbContext.</param>
        /// <param name="ignoreQueryFilters">Whether to ignore global query filters.</param>
        /// <param name="includes">Navigation properties to include.</param>
        /// <returns>An <see cref="IQueryable{T}"/> representing the query.</returns>
        public IQueryable<T> Query(Expression<Func<T, bool>>? filter = null, bool tracked = true, bool ignoreQueryFilters = false, params Expression<Func<T, object>>[] includes)
        {
            _logger.LogDebug("Querying {Entity} | Tracked={Tracked} | IgnoreFilters={IgnoreFilters} | Includes={IncludeCount}",
                typeof(T).Name, tracked, ignoreQueryFilters, includes?.Length ?? 0);

            return _repo.Query(filter, tracked, ignoreQueryFilters, includes);
        }

        /// <summary>
        /// Retrieves all entities that match the optional filter.
        /// </summary>
        /// <param name="filter">Optional expression to filter entities.</param>
        /// <param name="ignoreQueryFilters">Whether to ignore global query filters.</param>
        /// <param name="tracked">Whether the entities should be tracked by the DbContext.</param>
        /// <param name="includes">Navigation properties to include.</param>
        /// <returns>A collection of entities.</returns>
        public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null, bool ignoreQueryFilters = false, bool tracked = true, params Expression<Func<T, object>>[] includes)
        {
            _logger.LogInformation("Getting all {Entity} | Tracked={Tracked} | IgnoreFilters={IgnoreFilters}",
                typeof(T).Name, tracked, ignoreQueryFilters);

            return await _repo.GetAllAsync(filter, ignoreQueryFilters, tracked, includes);
        }

        /// <summary>
        /// Retrieves a paged list of entities along with the total count before pagination.
        /// </summary>
        /// <param name="filter">Optional expression to filter entities.</param>
        /// <param name="tracked">Whether the entities should be tracked by the DbContext.</param>
        /// <param name="ignoreQueryFilters">Whether to ignore global query filters.</param>
        /// <param name="pageNumber">Page number (1-based).</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <param name="includes">Navigation properties to include.</param>
        /// <returns>
        /// A tuple containing the paged items and the total count.
        /// </returns>
        public async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(Expression<Func<T, bool>>? filter = null,
            bool tracked = false,
            bool ignoreQueryFilters = false,
            int pageNumber = 1,
            int pageSize = 10,
            params Expression<Func<T, object>>[] includes)
        {
            _logger.LogInformation("Getting paged {Entity} | Page={Page} | Size={Size} | Tracked={Tracked}",
                typeof(T).Name, pageNumber, pageSize, tracked);

            return await _repo.GetPagedAsync(filter, tracked, ignoreQueryFilters, pageNumber, pageSize, includes);
        }

        /// <summary>
        /// Retrieves a single entity that matches the specified filter.
        /// </summary>
        /// <param name="filter">Expression used to find the entity.</param>
        /// <param name="tracked">Whether the entity should be tracked by the DbContext.</param>
        /// <param name="includes">Navigation properties to include.</param>
        /// <returns>
        /// The matching entity, or <c>null</c> if no entity is found.
        /// </returns>
        public async Task<T?> GetAsync(Expression<Func<T, bool>>? filter = null, bool tracked = true, params Expression<Func<T, object>>[] includes)
        {
            _logger.LogInformation("Getting single {Entity} | Tracked={Tracked}", typeof(T).Name, tracked);

            return await _repo.GetAsync(filter, tracked, includes);
        }

        /// <summary>
        /// Retrieves an entity by its identifier.
        /// </summary>
        /// <param name="id">The entity identifier.</param>
        /// <param name="includes">Navigation properties to include.</param>
        /// <returns>
        /// The entity if found; otherwise, <c>null</c>.
        /// </returns>
        public async Task<T?> GetByIdAsync(int id, params Expression<Func<T, object>>[] includes)
        {
            _logger.LogInformation("Getting {Entity} by Id={Id}", typeof(T).Name, id);

            return await _repo.GetByIdAsync(id, includes);
        }

        /// <summary>
        /// Determines whether any entity exists that matches the given predicate.
        /// </summary>
        /// <param name="predicate">Condition to check.</param>
        /// <returns>
        /// <c>true</c> if at least one entity exists; otherwise, <c>false</c>.
        /// </returns>
        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            _logger.LogDebug("Checking existence of {Entity}", typeof(T).Name);

            return await _repo.ExistsAsync(predicate);
        }

        /// <summary>
        /// Adds a new entity to the data store.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        public async Task AddAsync(T entity)
        {
            _logger.LogInformation("Adding new {Entity}", typeof(T).Name);

            await _repo.AddAsync(entity);
        }

        /// <summary>
        /// Updates an existing entity in the data store.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        public async Task UpdateAsync(T entity)
        {
            _logger.LogInformation("Updating {Entity}", typeof(T).Name);

            await _repo.UpdateAsync(entity);
        }

        /// <summary>
        /// Deletes an entity using soft delete if supported,
        /// otherwise performs a physical delete.
        /// </summary>
        /// <param name="entity">The entity to delete.</param>
        public async Task DeleteAsync(T entity)
        {
            _logger.LogWarning("Soft deleting {Entity}", typeof(T).Name);

            await _repo.DeleteAsync(entity);
        }

        /// <summary>
        /// Permanently deletes an entity from the data store,
        /// bypassing soft-delete behavior.
        /// </summary>
        /// <param name="entity">The entity to delete.</param>
        public async Task HardDeleteAsync(T entity)
        {
            _logger.LogWarning("Hard deleting {Entity}", typeof(T).Name);

            await _repo.HardDeleteAsync(entity);
        }

        /// <summary>
        /// Persists all pending changes to the data store.
        /// </summary>
        /// <returns>The number of affected records.</returns>
        public async Task<int> SaveChangesAsync()
        {
            _logger.LogDebug("Saving changes for {Entity}", typeof(T).Name);

            return await _unitOfWork.SaveChanges();
        }
    }
}
