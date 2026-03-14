using Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class EmailInfoDto
    {
        public string OrderNumber { get; set; } = string.Empty;
        public string FName { get; set; } = string.Empty;
        public string LName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Governorate { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public IEnumerable<CartDto> Items { get; set; } = new List<CartDto>();
        public decimal TotalPrice { get; set; }
    }
}
