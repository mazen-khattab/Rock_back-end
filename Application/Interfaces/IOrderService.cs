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
        Task<ApiResponse<(AuthServiceResponse? authResponse, CheckoutResponseDto checkoutResponse)>> ProcessCheckoutAsync(CheckoutRequestDto request, string? userId);
        Task<ApiResponse<List<Order>>> OrderHistory(int userId);
    }
}
