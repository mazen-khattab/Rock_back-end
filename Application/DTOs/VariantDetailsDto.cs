using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class VariantDetailsDto : BaseVariantDto
    {
        public List<ImagesDto> ImagesDtos { get; set; } = new();
    }
}
