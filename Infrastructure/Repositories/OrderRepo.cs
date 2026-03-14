using Core.Entities;
using Core.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Repositories
{
    public class OrderRepo : Repo<Order>, IOrderRepo
    {
        public OrderRepo(AppDbContext appDbContext) : base(appDbContext) { }


        public async Task<string> GenerateOrderNumberAsync(long createdAt) => $"ORD-{createdAt}";
        public async Task<bool> OrderNumberExistsAsync(string orderNumber) => await ExistsAsync(o => o.Number == orderNumber);
    }
}
