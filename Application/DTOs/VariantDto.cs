using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class VariantDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ColorName { get; set; } = string.Empty;
        public string HexCode { get; set; } = string.Empty;
        public string SizeName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int Reserved { get; set; }
    }
}
