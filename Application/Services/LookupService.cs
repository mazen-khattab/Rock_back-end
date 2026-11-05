using System.Linq.Expressions;
using Application.Responses;
using Core.Entities;
using Core.Enum;
using Core.ExceptionsTypes;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public abstract class LookupService<T> : Services<T> where T : BaseEntity
    {
        protected LookupService(IUnitOfWork unitOfWork, IRepo<T> repo, ILogger<Services<T>> logger)
            : base(unitOfWork, repo, logger) { }

        protected async Task<ApiResponse<int>> DeleteUnusedLookupAsync(int id, Expression<Func<T, bool>> isUnused,
            string entityName, string conflictMessage)
        {
            _logger.LogInformation("Deleting unused {Entity} with ID: {Id}", entityName, id);

            try
            {
                // No entity graph is loaded or tracked; the database applies the condition atomically.
                int deleted = await _repo.DeleteIfUnusedAsync(id, isUnused);
                if (deleted > 0)
                {
                    _logger.LogInformation("{Entity} with ID: {Id} permanently deleted", entityName, id);
                    return new() { IsSuccess = true, Data = id, Message = $"{entityName} deleted successfully." };
                }

                if (!await Query(e => e.Id == id, tracked: false).AnyAsync())
                {
                    _logger.LogWarning("{Entity} with ID: {Id} was not found for deletion", entityName, id);
                    return new() { IsSuccess = false, ErrorType = ErrorType.NotFound, Message = $"{entityName} not found." };
                }
            }
            catch (EntityInUseException ex)
            {
                _logger.LogWarning(ex, "A concurrent reference prevented deletion of {Entity} with ID: {Id}", entityName, id);
            }

            _logger.LogWarning("Cannot delete {Entity} with ID: {Id} because it is in use", entityName, id);
            return new() { IsSuccess = false, ErrorType = ErrorType.Conflict, Message = conflictMessage };
        }

        protected async Task<ApiResponse<TDto>> SaveLookupUpdateAsync<TDto>(int id, string entityName, Func<TDto> map)
        {
            try
            {
                // A single save commits the entity fields and both translations together.
                await SaveChangesAsync();
                _logger.LogInformation("{Entity} with ID: {Id} updated successfully", entityName, id);
                return new() { IsSuccess = true, Data = map(), Message = $"{entityName} updated successfully." };
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "{Entity} with ID: {Id} changed or was deleted during update", entityName, id);
                return new()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.ConcurrencyConflict,
                    Message = $"{entityName} changed during the update. Refresh and try again."
                };
            }
        }
    }
}
