using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class CheckoutResponseDto
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
