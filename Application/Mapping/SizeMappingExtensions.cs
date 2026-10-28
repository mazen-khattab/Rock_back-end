using Application.DTOs;
using Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Mapping
{
    public static class SizeMappingExtensions
    {
        public static SizeDto ToDto(this Size size)
        {
            ArgumentNullException.ThrowIfNull(size);

            return new SizeDto()
            {
                Id = size.Id,
                Name = size.Name ?? string.Empty,
                SortOrder = size.SortOrder,
                IsActive = size.IsActive
            };
        }

        public static List<SizeDto> ToDtoList(this IEnumerable<Size> sizes)
        {
            ArgumentNullException.ThrowIfNull(sizes);

            return sizes.Select(s => s.ToDto()).ToList();
        }

        public static SizeDetailsDto ToDetailsDto(this Size size)
        {
            ArgumentNullException.ThrowIfNull(size);

            return new SizeDetailsDto()
            {
                Name = size.Name ?? string.Empty,
                SortOrder = size.SortOrder,
                IsActive = !size.IsActive
            };
        }

        public static SizeLookupDto ToLookupDto(this Size lookup)
        {
            ArgumentNullException.ThrowIfNull(lookup);

            return new SizeLookupDto
            {
                Id = lookup.Id,
                Name = lookup.Name
            };
        }

        public static List<SizeLookupDto> ToListLookupDto(this IEnumerable<Size> lookup)
        {
            ArgumentNullException.ThrowIfNull(lookup);
            return lookup.Select(s => s.ToLookupDto()).ToList();
        }
    }
}
