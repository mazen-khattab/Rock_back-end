using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Core.Interfaces;

namespace Core.Entities
{
    public class Role : IdentityRole<int>, ISoftDelete, IAuditableEntity
    {
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
