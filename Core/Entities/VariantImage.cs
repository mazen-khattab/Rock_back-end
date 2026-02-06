using System;

namespace Core.Entities
{
    public class VariantImage : BaseEntity
    {
        public int VariantId { get; set; }
        public int MediaAssetId { get; set; }
        public int SortOrder { get; set; }

        public Variant Variant { get; set; } = null!;
        public MediaAsset MediaAsset { get; set; } = null!;
    }
}
