using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Application.DTOs
{
    public class ProductDetailsAdminDto
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Id { get; set; }
        public int CategoryId { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string DescriptionAr { get; set; } = string.Empty;
        public string DescriptionEn { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string SlugAr { get; set; } = string.Empty;
        public string SlugEn { get; set; } = string.Empty;
        public string MetaDescriptionAr { get; set; } = string.Empty;
        public string MetaDescriptionEn { get; set; } = string.Empty;
        public string MetaTitleAr { get; set; } = string.Empty;
        public string MetaTitleEn { get; set; } = string.Empty;
        public List<VariantsDetailsAdminDto> Variants { get; set; } = new List<VariantsDetailsAdminDto>();
    }
}
