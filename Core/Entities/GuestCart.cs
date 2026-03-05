using System;
using System.Runtime.Intrinsics.Arm;

namespace Core.Entities
{
    public class GuestCart : BaseCart
    {
        public DateTime ExpireAt { get; set; }
        public string GuestId { get; set; } = null!;
    }
}
