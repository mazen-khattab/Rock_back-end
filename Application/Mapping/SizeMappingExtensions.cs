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
            if (size == null)
            {
                throw new ArgumentNullException(nameof(Size));
            }

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
            if (sizes is null)
            {
                throw new ArgumentNullException(nameof(Color));
            }

            return sizes.Select(s => s.ToDto()).ToList();
        }

        public static SizeDetailsDto ToDetailsDto(this Size size)
        {
            if (size == null)
            {
                throw new ArgumentNullException(nameof(Size));
            }

            return new SizeDetailsDto()
            {
                Name = size.Name ?? string.Empty,
                SortOrder = size.SortOrder,
                IsActive = !size.IsActive
            };
        }

        public static SizeLookupDto ToLookupDto(this Size lookup)
        {
            if (lookup == null)
            {
                throw new ArgumentNullException(nameof(Color));
            }

            return new SizeLookupDto
            {
                Id = lookup.Id,
                Name = lookup.Name
            };
        }

        public static List<SizeLookupDto> ToListLookupDto(this IEnumerable<Size> lookup)
        {
            if (lookup == null)
            {
                throw new ArgumentNullException(nameof(Color));
            }
            return lookup.Select(s => s.ToLookupDto()).ToList();
        }
    }
}
