using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class VariantDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ColorName { get; set; } = null!;
        public string HexCode { get; set; } = null!;
        public string SizeName { get; set; } = null!;
        public int Quantity { get; set; }
        public int Reserved { get; set; }
        public List<VariantImagesDto> ImagesDtos { get; set; } = new List<VariantImagesDto>();
        //public int ColorId { get; set; }
        //public int SizeId { get; set; }
    }
}
