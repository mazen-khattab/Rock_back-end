using Application.DTOs;
using Application.Interfaces;
using Application.Responses;
using Core.ExceptionsTypes;
using Core.Settings;
using Microsoft.AspNetCore.Authorization;
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
        private readonly IMapper _mapper;
        private readonly JwtSettings _jwtSettings;

        public OrderController(IOrderService orderService, ILogger<OrderController> logger, IMapper mapper, IOptions<JwtSettings> jwtSettings)
        {
            _orderService = orderService;
            _logger = logger;
            _mapper = mapper;
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
                    IsEssential = true,
                    Expires = DateTime.Now.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
                };

                var refreshTokenCookie = new CookieOptions()
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    IsEssential = true,
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

        [HttpPost]
        [Route("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequestDto request)
        {
            _logger.LogInformation("Checkout endpoint called with IdempotencyKey: {IdempotencyKey}, Email: {Email}",
                request.IdempotencyKey, request.Email);

            // retrieve user ID from claims if available (for authenticated users)
            string? userId = GetUserId();
            _logger.LogInformation("UserId retrieved from claims: {userId}", userId);

            // Process checkout
            var response = await _orderService.ProcessCheckoutAsync(request, userId);

            if (!response.IsSucess)
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
        

        [HttpGet]
        [Authorize]
        [Route("OrderHistory/{langId}")]
        public async Task<IActionResult> OrderHistory(int langId)
        {
            _logger.LogInformation("OrderHistory endpoint called with langId: {langId}", langId);

            var userId = GetUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("user does not authorized");
            }

            var orders = await _orderService.OrderHistory(int.Parse(userId));
            var ordersDto = _mapper.MapToDtoList(orders.Data, langId);

            return Ok(ordersDto);
        }
    }
}
