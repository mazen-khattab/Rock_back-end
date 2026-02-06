using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Entities
{
    public class Color : BaseEntity
    {
        public string HexCode { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        public ICollection<ColorTranslation> Translations { get; set; } = new List<ColorTranslation>();
        public ICollection<Variant> Variants { get; set; } = new List<Variant>();
    }
}
