using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
    {
        public void Configure(EntityTypeBuilder<MediaAsset> builder)
        {
            builder.ToTable("MediaAssets");

            builder.HasKey(ma => ma.Id);

            builder.Property(ma => ma.FileName).IsRequired().HasMaxLength(255);
            builder.Property(ma => ma.FilePath).IsRequired().HasMaxLength(1000);
            builder.Property(ma => ma.AltText).HasMaxLength(500);
            builder.Property(ma => ma.MediaType).IsRequired().HasMaxLength(50);
            builder.Property(ma => ma.CreatedAt).IsRequired();
        }
    }
}
