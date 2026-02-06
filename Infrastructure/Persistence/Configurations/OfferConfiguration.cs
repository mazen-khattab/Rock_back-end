using System;
using System.Collections.Generic;
using System.Text;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class OfferConfiguration : IEntityTypeConfiguration<Offer>
    {
        public void Configure(EntityTypeBuilder<Offer> builder)
        {
            builder.ToTable("Offers");

            builder.HasKey(o => o.Id);

            builder.Property(o => o.DiscountType)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(o => o.DiscountValue)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(o => o.OfferType)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(o => o.Code).IsRequired().HasMaxLength(50);
            builder.Property(o => o.Priority).IsRequired();
            builder.Property(o => o.IsStackable).IsRequired();
            builder.Property(o => o.StartDate).IsRequired();
            builder.Property(o => o.EndDate).IsRequired();
            builder.Property(o => o.IsActive).IsRequired();
            builder.Property(o => o.CreatedAt).IsRequired();

            builder.HasMany(o => o.Translations)
                .WithOne(t => t.Offer)
                .HasForeignKey(t => t.OfferId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(o => o.ProductOffers)
                .WithOne(po => po.Offer)
                .HasForeignKey(po => po.OfferId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(o => o.CategorieOffers)
                .WithOne(co => co.Offer)
                .HasForeignKey(co => co.OfferId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
