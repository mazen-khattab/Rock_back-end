using System;
using Core.Entities;
using Core.Interfaces;

namespace Core.Entities
{
    public class Review : BaseEntity
    {
        public int ProductId { get; set; }
        public int UserId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public bool IsApproved { get; set; }

        public Product Product { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
