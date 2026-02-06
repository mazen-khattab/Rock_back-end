using System;
using System.Collections.Generic;

namespace Core.Entities
{
    public class Variant : BaseEntity
    {
        public int ProductId { get; set; }
        public int ColorId { get; set; }
        public int SizeId { get; set; }
        public int Quantity { get; set; }
        public int Reserved { get; set; }

        public Product Product { get; set; } = null!;
        public Color Color { get; set; } = null!;
        public Size Size { get; set; } = null!;
            
        public ICollection<VariantImage> Images { get; set; } = new List<VariantImage>();
        public ICollection<UserCart> UserCarts { get; set; } = new List<UserCart>();
        public ICollection<GuestCart> GuestCarts { get; set; } = new List<GuestCart>();
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
    }
}
