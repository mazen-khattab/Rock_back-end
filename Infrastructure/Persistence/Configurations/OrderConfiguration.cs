using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders");

            builder.HasKey(o => o.Id);

            builder.Property(o => o.TotalPrice).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(o => o.FullAddress).IsRequired().HasMaxLength(1000);
            builder.Property(o => o.Governorate).IsRequired().HasMaxLength(200);
            builder.Property(o => o.City).IsRequired().HasMaxLength(200);

            builder.Property(o => o.Status)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(o => o.CreatedAt).IsRequired();

            builder.HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(o => o.OrderDetails)
                .WithOne(od => od.Order)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(o => o.InventoryTransactions)
                .WithOne(it => it.Order)
                .HasForeignKey(it => it.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasQueryFilter(o => !o.IsDeleted);
        }
    }
}
