using Microsoft.AspNetCore.Identity;
using Core.Interfaces;

namespace Core.Entities
{
    public class UserRole : IdentityUserRole<int>, ISoftDelete, IAuditableEntity
    {
        public User User { get; set; } = null!;
        public Role Role { get; set; } = null!;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
