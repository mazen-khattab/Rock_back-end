using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Entities
{
    public class OfferTranslation : BaseEntity
    {
        public int OfferId { get; set; }
        public int LanguageId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        public Offer Offer { get; set; } = null!;
        public Language Language { get; set; } = null!;
    }
}
