using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class ProductDetailsDto : BaseProductDto
    {
        public string Description { get; set; } = string.Empty;
        public string MetaTitle { get; set; } = string.Empty;
        public string MetaDescription { get; set; } = string.Empty;
        public List<VariantDetailsDto> Variants { get; set; } = new();
    }
}
