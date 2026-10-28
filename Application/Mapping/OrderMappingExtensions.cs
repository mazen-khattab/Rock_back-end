using Application.DTOs;
using Core.Entities;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Text;

namespace Application.Mapping
{
    public static class OrderMappingExtensions
    {
        public static List<OrderHistoryDto> ToHistoryDtoList(this IEnumerable<Order> orders, int languageId, string baseUrl)
        {
            ArgumentNullException.ThrowIfNull(orders);

            return orders.Select(o => o.ToHistoryDto(languageId, baseUrl)).ToList();
        }

        public static OrderHistoryDto ToHistoryDto(this Order order, int languageId, string baseUrl)
        {
            ArgumentNullException.ThrowIfNull(order);

            return new OrderHistoryDto()
            {
                OrderNumber = order.Number,
                Status = order.Status.ToString(),
                TotalPrice = order.TotalPrice,
                CreatedAt = order.CreatedAt,
                OrderDetails = order.OrderDetails?.ToDetailsDtoList(languageId, baseUrl) ?? new List<OrderDetailsDto>()
            };
        }
    }
}
