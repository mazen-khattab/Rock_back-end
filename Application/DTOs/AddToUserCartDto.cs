using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class AddToUserCartDto
    {
        public int VariantId { get; set; }
        public int Quantity { get; set; }
    }
}
