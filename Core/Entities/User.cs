using Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace Core.Entities
{
    public class User : IdentityUser<int>, ISoftDelete
    {
        public string Fname { get; set; } = string.Empty;
        public string Lname { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }

        public ICollection<UserCart> UserCarts { get; set; } = new List<UserCart>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

        // Navigation for identity user-role relationship
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
