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
        public static OrdersAdminDto ToAdminDto(this Order order)
        {
            ArgumentNullException.ThrowIfNull(order);

            return new OrdersAdminDto
            {
                Id = order.Id,
                OrderNumber = order.Number,
                CustomerName = $"{order.User?.Fname} {order.User?.Lname}".Trim(),
                city = order.City,
                Status = order.Status.ToString(),
                Created = order.CreatedAt
            };
        }

        public static OrderDetailsAdminDto ToAdminDetailsDto(this Order order, int languageId, string baseUrl)
        {
            ArgumentNullException.ThrowIfNull(order);

            return new OrderDetailsAdminDto
            {
                Id = order.Id,
                OrderNumber = order.Number,
                UserId = order.UserId,
                CustomerName = $"{order.User?.Fname} {order.User?.Lname}".Trim(),
                Email = order.User?.Email ?? string.Empty,
                Phone = order.User?.PhoneNumber ?? string.Empty,
                // Delivery information comes from the order, even if the user's address changes.
                Governorate = order.Governorate,
                City = order.City,
                FullAddress = order.FullAddress,
                Status = order.Status.ToString(),
                TotalPrice = order.TotalPrice,
                CreatedAt = order.CreatedAt,
                OrderDetails = order.OrderDetails.OrderBy(od => od.Id)
                    .Select(od => od.ToAdminDetailsDto(languageId, baseUrl)).ToList()
            };
        }

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
