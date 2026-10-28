using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Application.DTOs
{
    public class VariantsDetailsAdminDto
    {
        public int Id { get; set; }
        public int SizeId { get; set; }
        public int ColorId { get; set; }
        public int Quantity { get; set; }
        public List<ImagesAdminDto> Images { get; set; } = new List<ImagesAdminDto>();
    }
}
