using System;

namespace Core.Entities
{
    public class GuestCart : BaseEntity
    {
        public int VariantId { get; set; }
        public int Quantity { get; set; }
        public DateTimeOffset ExpireAt { get; set; }
        public int GuestId { get; set; }

        public Variant Variant { get; set; } = null!;
    }
}
