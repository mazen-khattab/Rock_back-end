using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class OrderDetailsDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal OriginalPrice { get; set; }
        public string ColorName { get; set; } = string.Empty;
        public string HexCode { get; set; } = string.Empty;
        public string SizeName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Image { get; set; } = string.Empty;
    }
}
