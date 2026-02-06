using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class OfferTranslationConfiguration : IEntityTypeConfiguration<OfferTranslation>
    {
        public void Configure(EntityTypeBuilder<OfferTranslation> builder)
        {
            builder.ToTable("OfferTranslations");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Title).IsRequired().HasMaxLength(200);
            builder.Property(t => t.Description).HasMaxLength(2000);

            builder.HasIndex(t => new { t.OfferId, t.LanguageId }).IsUnique();

            builder.HasOne(t => t.Offer)
                .WithMany(o => o.Translations)
                .HasForeignKey(t => t.OfferId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(t => t.Language)
                .WithMany(l => l.OfferTranslations)
                .HasForeignKey(t => t.LanguageId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
