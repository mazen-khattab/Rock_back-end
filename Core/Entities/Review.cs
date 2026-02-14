using System;
using Core.Entities;

namespace Core.Entities
{
    public class Review : BaseEntity
    {
        public int ProductId { get; set; }
        public int UserId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public Product Product { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
