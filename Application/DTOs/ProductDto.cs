using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal OriginalPrice { get; set; }
        public string Slug { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
