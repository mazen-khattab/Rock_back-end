using Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Interfaces
{
    public interface IOrderRepo : IRepo<Order>
    {
        Task<string> GenerateOrderNumberAsync();
        Task<bool> OrderNumberExistsAsync(string orderNumber);
    }
}
