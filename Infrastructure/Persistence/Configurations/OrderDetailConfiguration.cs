using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class OrderDetailConfiguration : IEntityTypeConfiguration<OrderDetail>
    {
        public void Configure(EntityTypeBuilder<OrderDetail> builder)
        {
            builder.ToTable("OrderDetails");

            builder.HasKey(od => od.Id);

            builder.Property(od => od.Quantity).IsRequired();
            builder.Property(od => od.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(od => od.Discount).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(od => od.TotalPrice).HasColumnType("decimal(18,2)").IsRequired();

            builder.HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(od => od.Variant)
                .WithMany(v => v.OrderDetails)
                .HasForeignKey(od => od.VariantId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
