using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Entities
{
    public class IdempotencyRecord : BaseEntity
    {
        public string IdempotencyKey { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string ResponseData { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
