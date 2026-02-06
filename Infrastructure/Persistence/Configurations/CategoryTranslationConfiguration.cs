using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class CategoryTranslationConfiguration : IEntityTypeConfiguration<CategoryTranslation>
    {
        public void Configure(EntityTypeBuilder<CategoryTranslation> builder)
        {
            builder.ToTable("CategoryTranslations");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasIndex(t => new { t.CategoryId, t.LanguageId }).IsUnique();

            builder.HasOne(t => t.Category)
                .WithMany(c => c.Translations)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(t => t.Language)
                .WithMany(l => l.CategoryTranslations)
                .HasForeignKey(t => t.LanguageId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
