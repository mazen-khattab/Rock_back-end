using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Entities
{
    public class BaseCart : BaseEntity
    {
        public int VariantId { get; set; }
        public int Quantity { get; set; }

        public Variant Variant { get; set; } = null!;
    }
}
