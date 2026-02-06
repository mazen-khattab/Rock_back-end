using Core.Interfaces;
using System;
using System.Collections.Generic;
using Core.Enums;

namespace Core.Entities
{
    public class Order : BaseEntity, ISoftDelete
    {
        public int UserId { get; set; }
        public decimal TotalPrice { get; set; }
        public string FullAddress { get; set; } = string.Empty;
        public string Governorate { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public OrderStatus Status { get; set; }
        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public User User { get; set; } = null!;
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
    }
}
