using Application.DTOs;
using Application.Interfaces;
using Application.Mapping;
using Application.Responses;
using Core.Entities;
using Core.Enum;
using Core.Interfaces;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class SizeService : Services<Size>, ISizeService
    {
        public SizeService(IUnitOfWork unitOfWork, IRepo<Size> repo, ILogger<SizeService> logger) : base(unitOfWork, repo, logger) { }

        public async Task<ApiResponse<(IEnumerable<SizeDto> Items, int TotalCount)>> GetAll()
        {
            _logger.LogInformation("GetAll sizes requested");

            var (items, totalCount) = await GetPagedAsync();
            _logger.LogInformation("Successfully retrieved {Count} sizes", items.Count());

            var sizesDto = items.ToDtoList() ?? new();
            return new()
            {
                IsSuccess = true,
                Data = (sizesDto, totalCount),
                Message = "Sizes retrieved successfully."
            };

        }

        public async Task<ApiResponse<SizeDetailsDto>> GetById(int sizeId)
        {
            _logger.LogInformation("GetSizeById requested for SizeID: {SizeId}", sizeId);

            var size = await GetByIdAsync(sizeId);

            if (size == null)
            {
                _logger.LogWarning("Size with ID: {SizeId} was not found.", sizeId);

                return new()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.NotFound,
                    Message = "Size not found."
                };
            }

            _logger.LogInformation("Successfully retrieved size ID: {SizeId}", sizeId);

            var sizeDetailsDto = size.ToDetailsDto() ?? new();

            return new()
            {
                IsSuccess = true,
                Data = sizeDetailsDto,
                Message = "Size retrieved successfully.",
            };
        }

        public async Task<ApiResponse<IEnumerable<SizeLookupDto>>> Lookup()
        {
            _logger.LogInformation("Size lookup requested");

            var lookup = await GetAllAsync();
            _logger.LogInformation("Successfully retrieved size lookups");

            var lookupDto = lookup.ToListLookupDto();

            return new()
            {
                IsSuccess = true,
                Message = "Size lookups retrieved successfully.",
                Data = lookupDto,
            };
        }
    }
}
