using System;

namespace Core.Entities
{
    public class UserCart : BaseCart
    {
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }

        public User User { get; set; } = null!;
    }
}
