using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class AddToGuestCartDto
    {
        public string GuestId { get; set; } = null!;
        public int VariantId { get; set; }
        public int Quantity { get; set; }
    }
}
