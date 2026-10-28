using Core.Interfaces;
using System;
using System.Collections.Generic;

namespace Core.Entities
{
    public class Product : BaseEntity, ISoftDelete, IAuditableEntity
    {
        public int CategoryId { get; set; }
        public decimal Price { get; set; }
        public decimal OriginalPrice { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public Category Category { get; set; } = null!;
        public ICollection<ProductTranslation> Translations { get; set; } = new List<ProductTranslation>();
        public ICollection<Variant> Variants { get; set; } = new List<Variant>();
        public ICollection<ProductOffer> ProductOffers { get; set; } = new List<ProductOffer>();
    }
}
