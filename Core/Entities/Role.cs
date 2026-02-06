using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Core.Interfaces;

namespace Core.Entities
{
    public class Role : IdentityRole<int>, ISoftDelete
    {
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public bool IsDeleted { get; set; }
    }
}
