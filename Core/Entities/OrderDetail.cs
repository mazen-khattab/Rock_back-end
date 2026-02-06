using System;

namespace Core.Entities
{
    public class OrderDetail : BaseEntity
    {
        public int OrderId { get; set; }
        public int VariantId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public decimal TotalPrice { get; set; }

        public Order Order { get; set; } = null!;
        public Variant Variant { get; set; } = null!;
    }
}
