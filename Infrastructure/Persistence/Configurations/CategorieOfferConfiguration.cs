using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class CategorieOfferConfiguration : IEntityTypeConfiguration<CategorieOffer>
    {
        public void Configure(EntityTypeBuilder<CategorieOffer> builder)
        {
            builder.ToTable("CategorieOffers");

            builder.HasKey(co => co.Id);

            builder.HasIndex(co => new { co.OfferId, co.CategoryId }).IsUnique();

            builder.HasOne(co => co.Offer)
                .WithMany(o => o.CategorieOffers)
                .HasForeignKey(co => co.OfferId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(co => co.Category)
                .WithMany(c => c.CategorieOffers)
                .HasForeignKey(co => co.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
