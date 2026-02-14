using System;
using System.Collections.Generic;

namespace Core.Entities
{
    public class MediaAsset : BaseEntity
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string? AltText { get; set; }
        public string MediaType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public ICollection<VariantImage> VariantImages { get; set; } = new List<VariantImage>();
    }
}
