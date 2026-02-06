using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class ColorConfiguration : IEntityTypeConfiguration<Color>
    {
        public void Configure(EntityTypeBuilder<Color> builder)
        {
            builder.ToTable("Colors");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.HexCode)
                .IsRequired()
                .HasMaxLength(7);

            builder.Property(c => c.IsActive)
                .IsRequired();

            builder.HasMany(c => c.Translations)
                .WithOne(t => t.Color)
                .HasForeignKey(t => t.ColorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(c => c.Variants)
                .WithOne(v => v.Color)
                .HasForeignKey(v => v.ColorId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
