using System;
using System.Collections.Generic;

namespace Core.Entities
{
    public class Language : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;

        public ICollection<CategoryTranslation> CategoryTranslations { get; set; } = new List<CategoryTranslation>();
        public ICollection<ProductTranslation> ProductTranslations { get; set; } = new List<ProductTranslation>();
        public ICollection<ColorTranslation> ColorTranslations { get; set; } = new List<ColorTranslation>();
        public ICollection<OfferTranslation> OfferTranslations { get; set; } = new List<OfferTranslation>();
    }
}
