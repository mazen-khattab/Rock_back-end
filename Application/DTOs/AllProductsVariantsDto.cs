using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class AllProductsVariantsDto : VariantDto
    {
        public VariantImagesDto ImagesDtos { get; set; } = null!;
    }
}
