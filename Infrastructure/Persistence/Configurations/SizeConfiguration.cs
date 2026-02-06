using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class SizeConfiguration : IEntityTypeConfiguration<Size>
    {
        public void Configure(EntityTypeBuilder<Size> builder)
        {
            builder.ToTable("Sizes");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Name).IsRequired().HasMaxLength(50);
            builder.Property(s => s.SortOrder).IsRequired();
            builder.Property(s => s.IsActive).IsRequired();

            builder.HasMany(s => s.Variants)
                .WithOne(v => v.Size)
                .HasForeignKey(v => v.SizeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
