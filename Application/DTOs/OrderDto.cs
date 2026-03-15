using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class OrderDto
    {
        public string OrderNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<OrderDetailsDto> OrderDetails { get; set; } = new();
    }
}
