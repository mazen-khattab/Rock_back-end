using System;

namespace Core.Entities
{
    public class GuestCart : BaseEntity
    {
        public int VariantId { get; set; }
        public int Quantity { get; set; }
        public DateTime ExpireAt { get; set; }
        public string GuestId { get; set; } = null!;

        public Variant Variant { get; set; } = null!;
    }
}
