using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class ColorLookupDto : BaseLookupDto
    {
        public string HexCode { get; set; } = string.Empty;
    }
}
