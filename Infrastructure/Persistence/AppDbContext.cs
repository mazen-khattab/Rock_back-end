using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Persistence
{
    public class AppDbContext : IdentityDbContext<User, Role, int,
        IdentityUserClaim<int>, UserRole, IdentityUserLogin<int>,
        IdentityRoleClaim<int>, IdentityUserToken<int>>
    {
        // DbSets for all entities in Core.Entities
        public DbSet<Color> Colors { get; set; } = null!;
        public DbSet<ColorTranslation> ColorTranslations { get; set; } = null!;
        public DbSet<Variant> Variants { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<ProductTranslation> ProductTranslations { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<CategoryTranslation> CategoryTranslations { get; set; } = null!;
        public DbSet<Language> Languages { get; set; } = null!;
        public DbSet<Offer> Offers { get; set; } = null!;
        public DbSet<OfferTranslation> OfferTranslations { get; set; } = null!;
        public DbSet<ProductOffer> ProductOffers { get; set; } = null!;
        public DbSet<CategorieOffer> CategorieOffers { get; set; } = null!;
        public DbSet<Size> Sizes { get; set; } = null!;
        public DbSet<VariantImage> VariantImages { get; set; } = null!;
        public DbSet<MediaAsset> MediaAssets { get; set; } = null!;
        public DbSet<UserCart> UserCarts { get; set; } = null!;
        public DbSet<GuestCart> GuestCarts { get; set; } = null!;
        public DbSet<OrderDetail> OrderDetails { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; } = null!;
        public DbSet<Review> Reviews { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
            modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
            modelBuilder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");
            modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
        }

        public override int SaveChanges()
        {
            foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Deleted && e.Entity is ISoftDelete))
            {
                var entity = (ISoftDelete)entry.Entity;
                entity.IsDeleted = true;
                entry.State = EntityState.Modified;
            }
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Deleted && e.Entity is ISoftDelete))
            {
                var entity = (ISoftDelete)entry.Entity;
                entity.IsDeleted = true;
                entry.State = EntityState.Modified;
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
