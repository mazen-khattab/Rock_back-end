using Application.DTOs;
using Application.Interfaces;
using Application.Mapping;
using Application.Responses;
using Azure;
using Core.ExceptionsTypes;
using Core.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Claims;

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

        private string GetUserId()
        {
            _logger.LogInformation("Retrieving user ID from claims in 'GetUserId' method");

            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("Unauthorize user");
        }

        private string BaseURL()
        {
            var baseURL = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            _logger.LogInformation("Base URL for product images: {BaseUrl}", baseURL);

            return baseURL;
        }


        private void GenerateCookies(AuthServiceResponse response)
        {
            _logger.LogInformation("Generating cookies for Access and Refresh tokens...");

            try
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    IsEssential = true
                };

                var accessTokenCookie = cookieOptions;
                accessTokenCookie.Expires = DateTime.Now.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

                var refreshTokenCookie = cookieOptions;
                refreshTokenCookie.Expires = DateTime.Now.AddDays(_jwtSettings.RefreshTokenExpirationDays);

                Response.Cookies.Append("accessToken", response.AccessToken, accessTokenCookie);
                Response.Cookies.Append("refreshToken", response.RefreshToken, refreshTokenCookie);

                _logger.LogInformation("Cookies generated and appended successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Something went wrong while generating the cookies.");
            }
        }

        [HttpPost]
        [Route("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequestDto request)
        {
            string baseUrl = BaseURL();

            _logger.LogInformation("Checkout endpoint called with IdempotencyKey: {IdempotencyKey}, Email: {Email}",
                request.IdempotencyKey, request.Email);

            // retrieve user ID from claims if available (for authenticated users)
            string? userId = GetUserId();
            _logger.LogInformation("UserId retrieved from claims: {userId}", userId);

            // Process checkout
            var response = await _orderService.ProcessCheckoutAsync(request, userId, baseUrl);

            if (!response.IsSuccess)
            {
                _logger.LogWarning("Checkout failed: {Message}", response.Message);

                var errorResponse = new ApiErrorResponse
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = response?.Message ?? "Checkout failed.",
                    Path = "api/Orders/checkout",
                    Method = HttpMethods.Post,
                    TraceId = HttpContext.TraceIdentifier
                };

                _logger.LogWarning("Returning error response for userId: {UserId} with StatusCode: {StatusCode}, Message: {Message}",
                    userId, errorResponse.StatusCode, errorResponse.Message);

                return BadRequest(errorResponse);
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
        

        [HttpGet]
        [Authorize]
        [Route("OrderHistory")]
        public async Task<IActionResult> OrderHistory()
        {
            string currentLang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            _logger.LogInformation("OrderHistory endpoint called with langCode: {CurrentLang}", currentLang);

            string baseUrl = BaseURL();
            var userId = int.Parse(GetUserId());

            var result = await _orderService.UserOrderHistory(userId, currentLang, baseUrl);

            if (result is not null && result.IsSuccess)
            {
                _logger.LogInformation("Order history retrieved successully for user: {UserID}, with landCode: {CurrentLang}", userId, currentLang);
                return Ok(result);
            }

            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Failed to retrieve order history.",
                Path = "api/Orders/OrderHistory",
                Method = HttpMethods.Get,
                TraceId = HttpContext.TraceIdentifier
            };

            _logger.LogWarning("Returning error response for userId: {UserId} with StatusCode: {StatusCode}, Message: {Message}",
                userId, errorResponse.StatusCode, errorResponse.Message);

            return BadRequest(errorResponse);
        }
    }
}
