using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class AllProductsAdminDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int Reserved { get; set; }
        public int Available { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public IReadOnlyList<string> Sizes { get; set; } = new List<string>();
        public string ImageUrl { get; set; } = string.Empty;
    }
}
