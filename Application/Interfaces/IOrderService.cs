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
        Task<bool> OrderNumberExistsAsync(string orderNumber);
        Task<ApiResponse<(AuthServiceResponse? authResponse, CheckoutResponseDto response)>> ProcessCheckoutAsync(CheckoutRequestDto request, string? userId);
    }
}
