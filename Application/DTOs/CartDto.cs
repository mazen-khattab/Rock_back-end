using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class CartDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int VariantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public VariantImagesDto imagesDtos { get; set; } = null!;
        public string Color { get; set; } = string.Empty;
        public string HexCode { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public int Reserved { get; set; }
        public int Quantity { get; set; }
    }
}
