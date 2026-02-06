using System;
using System.Collections.Generic;

namespace Core.Entities
{
    public class Category : BaseEntity
    {
        public bool IsActive { get; set; }

        public ICollection<CategoryTranslation> Translations { get; set; } = new List<CategoryTranslation>();
        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<CategorieOffer> CategorieOffers { get; set; } = new List<CategorieOffer>();
    }
}
