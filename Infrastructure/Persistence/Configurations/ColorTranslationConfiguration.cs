using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class ColorTranslationConfiguration : IEntityTypeConfiguration<ColorTranslation>
    {
        public void Configure(EntityTypeBuilder<ColorTranslation> builder)
        {
            builder.ToTable("ColorTranslations");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasIndex(t => new { t.ColorId, t.LanguageId }).IsUnique();

            builder.HasOne(t => t.Color)
                .WithMany(c => c.Translations)
                .HasForeignKey(t => t.ColorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(t => t.Language)
                .WithMany(l => l.ColorTranslations)
                .HasForeignKey(t => t.LanguageId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
