using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Entities
{
    public class RefreshToken : BaseEntity
    {
        public int UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTimeOffset ExpDate { get; set; }

        public User User { get; set; } = null!;
    }
}
