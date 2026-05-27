using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class ProductDetailsVariantsAdminDto
    {
        public int SizeId { get; set; }
        public int ColorId { get; set; }
        public int Quantity { get; set; }
        public List<ProductDetailsVariantImagesAdminDto> Images { get; set; } = new List<ProductDetailsVariantImagesAdminDto>();
    }
}
