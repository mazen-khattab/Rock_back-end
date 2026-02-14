using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class GuestCartOperationDto
    {
        public string GuestId { get; set; } = null!;
        public int VariantId { get; set; }
    }
}
