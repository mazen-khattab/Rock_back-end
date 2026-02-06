using Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class ProductsDto
    {
        public int Id { get; set; }
        public string Category { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal Price { get; set; }
        public decimal OriginalPrice { get; set; }
        public string Slug { get; set; } = null!;
        public string MetaTitle { get; set; } = null!;
        public string MetaDescription { get; set; } = null!;
        public List<VariantDto> Variants { get; set; } = new List<VariantDto>();
    }
}
