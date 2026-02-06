using System;

namespace Core.Entities
{
    public class ProductTranslation : BaseEntity
    {
        public int ProductId { get; set; }
        public int LanguageId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Slug { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }

        public Product Product { get; set; } = null!;
        public Language Language { get; set; } = null!;
    }
}
