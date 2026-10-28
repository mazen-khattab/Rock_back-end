using Application.DTOs;
using Application.Responses;
using Core.Entities;
using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IOrderService : IServices<Order>
    {
        Task<string> GenerateOrderNumberAsync();

        /// <summary>
        /// Processes checkout with complete order creation workflow including:
        /// - Transaction management
        /// - Idempotency check
        /// - User authentication handling
        /// - Guest cart merge
        /// - Order and OrderDetails creation
        /// - Variant reservation with stock validation
        /// - Inventory transaction recording
        /// </summary>
        /// <param name="request">Checkout request with cart, userId details</param>
        /// <returns>API response with order informations</returns>
        Task<ApiResponse<(AuthServiceResponse? authResponse, CheckoutResponseDto checkoutResponse)>> ProcessCheckoutAsync(CheckoutRequestDto request, string? userId, string baseUrl);

        /// <summary>
        /// Retrieves the full orders for a specific user.
        /// Includes nested details such as product variants, translations, and associated media.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose order history is being requested.</param>
        /// <param name="langId">The unique identifier of lang that the data will return with.</param>
        /// <returns>`
        /// An asynchronous task that returns an <see cref="IEnumerable{T}"/> containing a list of <see cref="Order"/> objects.
        /// </returns>
        Task<IEnumerable<Order>> FetchUserOrdersWithDetailsAsync(int userId, int langId);

        Task<ApiResponse<IEnumerable<OrderHistoryDto>>> UserOrderHistory(int userId, string langCode, string baseUrl);
    }
}
