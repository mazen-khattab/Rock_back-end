using Application.DTOs;
using Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Mapping
{
    public static class OrderMappingExtensions
    {
        public static List<OrderDto> ToDtoList(this IEnumerable<Order> orders, int languageId)
        {
            if (orders == null)
            {
                throw new ArgumentNullException(nameof(orders));
            }

            return orders.Select(o => o.ToDto(languageId)).ToList();
        }

        public static OrderDto ToDto(this Order order, int languageId)
        {
            if (order is null)
            {
                throw new ArgumentNullException(nameof(order));
            }

            return new OrderDto()
            {
                OrderNumber = order.Number,
                Status = order.Status.ToString(),
                TotalPrice = order.TotalPrice,
                CreatedAt = order.CreatedAt,
                OrderDetails = order.OrderDetails?.ToDetailsDtoList(languageId) ?? new List<OrderDetailsDto>()
            };
        }
    }
}
