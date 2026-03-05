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


        public async Task<string> GenerateOrderNumberAsync() => $"ORD-{Guid.NewGuid().ToString()}";
        public async Task<bool> OrderNumberExistsAsync(string orderNumber) => await _dbSet.AnyAsync(o => o.Number == orderNumber);
    }
}
