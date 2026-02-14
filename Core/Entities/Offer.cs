using System;
using System.Collections.Generic;
using Core.Enums;

namespace Core.Entities
{
    public class Offer : BaseEntity
    {
        public OfferDiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public OfferType OfferType { get; set; }
        public string Code { get; set; } = string.Empty;
        public int Priority { get; set; }
        public bool IsStackable { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<OfferTranslation> Translations { get; set; } = new List<OfferTranslation>();
        public ICollection<ProductOffer> ProductOffers { get; set; } = new List<ProductOffer>();
        public ICollection<CategorieOffer> CategorieOffers { get; set; } = new List<CategorieOffer>();
    }
}
