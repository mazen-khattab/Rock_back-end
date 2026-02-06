using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class GuestCartConfiguration : IEntityTypeConfiguration<GuestCart>
    {
        public void Configure(EntityTypeBuilder<GuestCart> builder)
        {
            builder.ToTable("GuestCarts");

            builder.HasKey(gc => gc.Id);

            builder.Property(gc => gc.Quantity).IsRequired();
            builder.Property(gc => gc.ExpireAt).IsRequired();
            builder.Property(gc => gc.GuestId).IsRequired();

            builder.HasOne(gc => gc.Variant)
                .WithMany(v => v.GuestCarts)
                .HasForeignKey(gc => gc.VariantId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
