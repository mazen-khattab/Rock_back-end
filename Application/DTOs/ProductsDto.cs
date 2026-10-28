using Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class ProductsDto : BaseProductDto
    {
        public List<VariantsDto> Variants { get; set; } = new();
    }
}
