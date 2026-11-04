using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.ToTable("OutboxMessages");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Type)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.Payload)
                .IsRequired();

            builder.Property(e => e.Error)
                .HasMaxLength(2000)
                .IsRequired(false);

            builder.HasIndex(e => new { e.ProcessedAt, e.RetryCount });
        }
    }
}
