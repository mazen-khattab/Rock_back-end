using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class VariantsDto : BaseVariantDto
    {
        public ImagesDto ImagesDtos { get; set; } = null!;
    }
}
