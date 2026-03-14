using Application.DTOs;
using Application.Responses;
using Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IOrderService
    {
        Task<string> GenerateOrderNumberAsync();
        Task<ApiResponse<(AuthServiceResponse? authResponse, CheckoutResponseDto checkoutResponse)>> ProcessCheckoutAsync(CheckoutRequestDto request, string? userId);
    }
}
