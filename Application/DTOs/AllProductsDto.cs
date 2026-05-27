using Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class AllProductsDto : ProductDto
    {
        public List<AllProductsVariantsDto> Variants { get; set; } = new();
    }
}
