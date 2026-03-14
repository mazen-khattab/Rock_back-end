using Application.DTOs;
using Application.Interfaces;
using Application.Responses;
using Core.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrderController> _logger;
        readonly JwtSettings _jwtSettings;

        public OrderController(IOrderService orderService, ILogger<OrderController> logger, IOptions<JwtSettings> jwtSettings)
        {
            _orderService = orderService;
            _logger = logger;
            _jwtSettings = jwtSettings.Value;
        }

        private string? GetUserId()
        {
            _logger.LogInformation("Retrieving user ID from claims in 'GetUserId' method");

            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        private void GenerateCookies(AuthServiceResponse response)
        {
            _logger.LogInformation("Generating cookies in 'GenerateCookies' method");

            try
            {
                var accessTokenCookie = new CookieOptions()
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.Now.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
                };

                var refreshTokenCookie = new CookieOptions()
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.Now.AddDays(_jwtSettings.RefreshTokenExpirationDays)
                };

                Response.Cookies.Append("accessToken", response.AccessToken, accessTokenCookie);
                Response.Cookies.Append("refreshToken", response.RefreshToken, refreshTokenCookie);

                _logger.LogInformation("Cookies generated successfully\n");
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Some thing went wrong while creating the cookies, {errer}", ex);
            }
        }

        /// <summary>
        /// Processes a complete checkout workflow including order creation, 
        /// inventory reservation, and transaction management.
        /// 
        /// Workflow:
        /// 1. Validates idempotency to prevent duplicate orders
        /// 2. Authenticates user (existing or guest login)
        /// 3. Merges guest cart if applicable
        /// 4. Validates inventory availability
        /// 5. Creates order and order details
        /// 6. Reserves product variants
        /// 7. Records inventory transactions
        /// 8. Commits or rolls back transaction atomically
        /// </summary>
        /// <param name="request">Checkout request containing user, cart, and shipping information</param>
        /// <returns>
        /// 200 OK: Order created successfully with OrderId, OrderNumber, TotalPrice, and CreatedAt timestamp
        /// 400 Bad Request: Validation errors or insufficient inventory
        /// 409 Conflict: Empty cart or other checkout conflicts
        /// 500 Internal Server Error: Database or processing errors
        /// </returns>
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequestDto request)
        {
            _logger.LogInformation("Checkout endpoint called with IdempotencyKey: {IdempotencyKey}, Email: {Email}",
                request.IdempotencyKey, request.Email);

            // retrieve user ID from claims if available (for authenticated users)
            string? userId = GetUserId();
            _logger.LogInformation("UserId retrieved from claims: {userId}", userId);

            // Process checkout
            var response = await _orderService.ProcessCheckoutAsync(request, userId);

            if (!response.isSucess)
            {
                _logger.LogWarning("Checkout failed: {Message}", response.Message);
                return BadRequest(response);
            }

            if (response.Data.authResponse is not null)
            {
                GenerateCookies(response.Data.authResponse);
            }

            var order = response.Data.checkoutResponse;

            _logger.LogInformation("Checkout completed successfully: OrderId: {OrderId}, OrderNumber: {OrderNumber}",
                order.OrderNumber, order.OrderNumber);

            return Ok(order);
        }
    }
}
