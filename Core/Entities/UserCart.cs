using System;

namespace Core.Entities
{
    public class UserCart : BaseEntity
    {
        public int UserId { get; set; }
        public int VariantId { get; set; }
        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; }

        public User User { get; set; } = null!;
        public Variant Variant { get; set; } = null!;
    }
}
