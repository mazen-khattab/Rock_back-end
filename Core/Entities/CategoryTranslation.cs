using System;

namespace Core.Entities
{
    public class CategoryTranslation : BaseEntity
    {
        public int CategoryId { get; set; }
        public int LanguageId { get; set; }
        public string Name { get; set; } = string.Empty;

        public Category Category { get; set; } = null!;
        public Language Language { get; set; } = null!;
    }
}
