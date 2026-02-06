using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class ProductOfferConfiguration : IEntityTypeConfiguration<ProductOffer>
    {
        public void Configure(EntityTypeBuilder<ProductOffer> builder)
        {
            builder.ToTable("ProductOffers");

            builder.HasKey(po => po.Id);

            builder.HasIndex(po => new { po.OfferId, po.ProductId }).IsUnique();

            builder.HasOne(po => po.Offer)
                .WithMany(o => o.ProductOffers)
                .HasForeignKey(po => po.OfferId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(po => po.Product)
                .WithMany(p => p.ProductOffers)
                .HasForeignKey(po => po.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
