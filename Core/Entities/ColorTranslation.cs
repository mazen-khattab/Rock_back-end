using System;

namespace Core.Entities
{
    public class ColorTranslation : BaseEntity
    {
        public int ColorId { get; set; }
        public int LanguageId { get; set; }
        public string Name { get; set; } = string.Empty;

        public Color Color { get; set; } = null!;
        public Language Language { get; set; } = null!;
    }
}
    