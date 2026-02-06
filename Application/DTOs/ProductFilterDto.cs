using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class ProductFilterDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public string? Category { get; set; } = null!;
        public string? Size { get; set; } = null!;
        public string? Color { get; set; } = null!;
    }
}
