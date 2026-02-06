using Core.Entities;
using Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
    {
        public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
        {
            builder.ToTable("InventoryTransactions");

            builder.HasKey(it => it.Id);

            builder.Property(it => it.TransactionType)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(it => it.Quantity).IsRequired();
            builder.Property(it => it.CreatedAt).IsRequired();

            builder.HasOne(it => it.Order)
                .WithMany(o => o.InventoryTransactions)
                .HasForeignKey(it => it.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(it => it.User)
                .WithMany(u => u.InventoryTransactions)
                .HasForeignKey(it => it.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(it => it.Variant)
                .WithMany(v => v.InventoryTransactions)
                .HasForeignKey(it => it.VariantId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
