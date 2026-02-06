using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class UserCartConfiguration : IEntityTypeConfiguration<UserCart>
    {
        public void Configure(EntityTypeBuilder<UserCart> builder)
        {
            builder.ToTable("UserCarts");

            builder.HasKey(uc => uc.Id);

            builder.Property(uc => uc.Quantity).IsRequired();
            builder.Property(uc => uc.CreatedAt).IsRequired();

            builder.HasOne(uc => uc.User)
                .WithMany(u => u.UserCarts)
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(uc => uc.Variant)
                .WithMany(v => v.UserCarts)
                .HasForeignKey(uc => uc.VariantId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
