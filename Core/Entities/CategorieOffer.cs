using System;

namespace Core.Entities
{
    public class CategorieOffer : BaseEntity
    {
        public int OfferId { get; set; }
        public int CategoryId { get; set; }

        public Offer Offer { get; set; } = null!;
        public Category Category { get; set; } = null!;
    }
}
