using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class OrdersAdminDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string city { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime Created { get; set; }
    }
}
