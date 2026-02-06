using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class VariantConfiguration : IEntityTypeConfiguration<Variant>
    {
        public void Configure(EntityTypeBuilder<Variant> builder)
        {
            builder.ToTable("Variants");

            builder.HasKey(v => v.Id);

            builder.Property(v => v.Quantity).IsRequired();
            builder.Property(v => v.Reserved).IsRequired();

            builder.HasOne(v => v.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(v => v.Color)
                .WithMany(c => c.Variants)
                .HasForeignKey(v => v.ColorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(v => v.Size)
                .WithMany(s => s.Variants)
                .HasForeignKey(v => v.SizeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(v => v.Images)
                .WithOne(i => i.Variant)
                .HasForeignKey(i => i.VariantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(v => v.UserCarts)
                .WithOne(uc => uc.Variant)
                .HasForeignKey(uc => uc.VariantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(v => v.GuestCarts)
                .WithOne(gc => gc.Variant)
                .HasForeignKey(gc => gc.VariantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(v => v.OrderDetails)
                .WithOne(od => od.Variant)
                .HasForeignKey(od => od.VariantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(v => v.InventoryTransactions)
                .WithOne(it => it.Variant)
                .HasForeignKey(it => it.VariantId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
