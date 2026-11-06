using API.Extensions;
using Application.DTOs;
using Application.Interfaces;
using Core.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController> _logger;
        private readonly JwtSettings _jwtSettings;

        public OrdersController(IOrderService orderService, ILogger<OrdersController> logger, IOptions<JwtSettings> jwtSettings)
        {
            _orderService = orderService;
            _logger = logger;
            _jwtSettings = jwtSettings.Value;
        }

        /// <summary>
        /// Lists orders across all customers for the admin panel, newest first.
        /// </summary>
        [HttpGet("admin")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> GetAdminOrders([FromQuery] OrderFilterDto filterDto)
        {
            _logger.LogInformation("Admin order list requested, page: {PageNumber}, size: {PageSize}", filterDto.PageNumber, filterDto.PageSize);
            return this.ToPagedApiResult(await _orderService.GetAllAdminOrders(filterDto), filterDto.PageNumber, filterDto.PageSize);
        }

        [HttpGet("admin/{id:int}")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> GetAdminOrderById(int id)
        {
            _logger.LogInformation("Admin order details requested for ID: {OrderId}", id);
            return this.ToApiResult(await _orderService.GetAdminOrderById(id, Request.GetLanguageCode(), Request.GetBaseUrl()));
        }

        /// <summary>
        /// Supports guest checkout; authenticated callers must supply a valid user-ID claim.
        /// </summary>
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequestDto request)
        {
            string? userId = User.GetOptionalUserId()?.ToString();
            _logger.LogInformation("Checkout requested with idempotency key: {IdempotencyKey}", request.IdempotencyKey);
            var result = await _orderService.ProcessCheckoutAsync(request, userId, Request.GetBaseUrl());

            // Checkout keeps its existing plain order response and optional sign-in cookies.
            return this.ToApiResult(result, response =>
            {
                if (response.Data.authResponse != null)
                    Response.SetAuthenticationCookies(response.Data.authResponse, _jwtSettings);
                _logger.LogInformation("Checkout completed for order number: {OrderNumber}", response.Data.checkoutResponse.OrderNumber);
                return Ok(response.Data.checkoutResponse);
            });
        }

        [HttpGet("OrderHistory")]
        [Authorize]
        public async Task<IActionResult> OrderHistory()
        {
            int userId = User.GetRequiredUserId();
            _logger.LogInformation("Order history requested for user ID: {UserId}", userId);
            return this.ToApiResult(await _orderService.UserOrderHistory(userId, Request.GetLanguageCode(), Request.GetBaseUrl()));
        }
    }
}
