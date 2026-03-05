using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Persistence.Configurations
{
    public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
    {
        public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
        {
            builder.ToTable("IdempotencyRecords");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.IdempotencyKey)
                .HasMaxLength(500)
                .IsRequired();

            builder.HasIndex(e => e.IdempotencyKey)
                .IsUnique();

            builder.Property(e => e.OrderId)
                .HasMaxLength(36);

            builder.Property(e => e.ResponseData)
                .IsRequired(false);

            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
