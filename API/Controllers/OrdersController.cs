using Application.DTOs;
using Application.Interfaces;
using API.Extensions;
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

        private string? GetUserId()
        {
            _logger.LogInformation("Retrieving user ID from claims in 'GetUserId' method");

            return User.FindFirstValue(ClaimTypes.NameIdentifier);
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
                var accessCookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.Now.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes) // Access Token Expiry
                };

                var refreshCookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.Now.AddDays(_jwtSettings.RefreshTokenExpirationDays) // Refresh Token Expiry
                };

                Response.Cookies.Append("accessToken", response.AccessToken, accessCookieOptions);
                Response.Cookies.Append("refreshToken", response.RefreshToken, refreshCookieOptions);

                _logger.LogInformation("Cookies generated and appended successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Something went wrong while generating the cookies.");
            }
        }

        /// <summary>
        /// Lists orders across all customers for the admin panel, newest first.
        /// </summary>
        [HttpGet("admin")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> GetAdminOrders([FromQuery] OrderFilterDto filterDto)
        {
            _logger.LogInformation("HTTP GET request received for api/orders/admin with PageNumber: {PageNumber}, PageSize: {PageSize}",
                filterDto.PageNumber, filterDto.PageSize);

            var result = await _orderService.GetAllAdminOrders(filterDto);
            if (result.IsSuccess)
            {
                var (items, totalCount) = result.Data;
                _logger.LogInformation("Returning {Count} admin orders out of {TotalCount}", items.Count(), totalCount);
                return Ok(new
                {
                    items,
                    totalCount,
                    pageNumber = filterDto.PageNumber,
                    pageSize = filterDto.PageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)filterDto.PageSize)
                });
            }

            int statusCode = result.ErrorType.ToHttpStatusCode();
            _logger.LogWarning("Failed to retrieve admin orders. Message: {Message}", result.Message);
            return StatusCode(statusCode, new ApiErrorResponse
            {
                StatusCode = statusCode,
                Message = result.Message,
                Path = Request.Path.Value ?? "api/Orders/admin",
                Method = HttpMethods.Get,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        /// <summary>
        /// Returns customer, delivery and purchased item details for one order.
        /// </summary>
        [HttpGet("admin/{id:int}")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> GetAdminOrderById(int id)
        {
            string langCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            _logger.LogInformation("HTTP GET request received for api/orders/admin/{OrderId} with LangCode: {LangCode}", id, langCode);

            var result = await _orderService.GetAdminOrderById(id, langCode, BaseURL());
            if (result.IsSuccess)
            {
                _logger.LogInformation("Returning admin order details for ID: {OrderId}", id);
                return Ok(result);
            }

            int statusCode = result.ErrorType.ToHttpStatusCode();
            _logger.LogWarning("Failed to retrieve admin order ID: {OrderId}. Message: {Message}", id, result.Message);
            return StatusCode(statusCode, new ApiErrorResponse
            {
                StatusCode = statusCode,
                Message = result.Message,
                Path = Request.Path.Value ?? $"api/Orders/admin/{id}",
                Method = HttpMethods.Get,
                TraceId = HttpContext.TraceIdentifier
            });
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
            var userId = int.Parse(GetUserId()!);

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
