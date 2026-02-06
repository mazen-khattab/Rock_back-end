using System;
using Core.Enums;

namespace Core.Entities
{
    public class InventoryTransaction : BaseEntity
    {
        public int? OrderId { get; set; }
        public int UserId { get; set; }
        public int VariantId { get; set; }
        public int Quantity { get; set; }
        public InventoryTransactionType TransactionType { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public Order? Order { get; set; }
        public User User { get; set; } = null!;
        public Variant Variant { get; set; } = null!;
    }
}
