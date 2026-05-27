using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class ProductDetailsVariantDto : VariantDto
    {
        public List<VariantImagesDto> ImagesDtos { get; set; } = new();
    }
}
