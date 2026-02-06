using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class VariantImageConfiguration : IEntityTypeConfiguration<VariantImage>
    {
        public void Configure(EntityTypeBuilder<VariantImage> builder)
        {
            builder.ToTable("VariantImages");

            builder.HasKey(vi => vi.Id);

            builder.Property(vi => vi.SortOrder).IsRequired();

            builder.HasOne(vi => vi.Variant)
                .WithMany(v => v.Images)
                .HasForeignKey(vi => vi.VariantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(vi => vi.MediaAsset)
                .WithMany(ma => ma.VariantImages)
                .HasForeignKey(vi => vi.MediaAssetId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
