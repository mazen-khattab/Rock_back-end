using API.Extensions;
using Application.DTOs;
using Application.Interfaces;
using Application.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartsController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly ILogger<CartsController> _logger;

        public CartsController(ICartService cartService, ILogger<CartsController> logger)
        {
            _cartService = cartService;
            _logger = logger;
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

        private string GetLangCode()
        {
            _logger.LogInformation("Getting the current language code from the request");
            string currentLang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            _logger.LogInformation("Current language code: {LangCode}", currentLang);

            return currentLang;
        }


        [HttpGet]
        [Authorize]
        [Route("GetUserCart")]  
        public async Task<IActionResult> GetUserCart()
        {
            string currentLang = GetLangCode();
            string baseURL = BaseURL();

            int userId = int.Parse(GetUserId());
            _logger.LogDebug("Getting the cart of user: {0}", userId);

            var result = await _cartService.GetUserCart(userId, currentLang, baseURL);

            if (result is not null && result.IsSuccess)
            {
                _logger.LogInformation("Successfully getting the cart for user {UserId}", userId);
                return Ok(result);
            }

            _logger.LogWarning("Failed to get the cart of user {UserId}: {Message}", userId, result?.Message ?? "");

            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Failed to get the cart of user.",
                Path = "api/Carts/GetUserCart",
                Method = HttpMethods.Get,
                TraceId = HttpContext.TraceIdentifier
            };

            return BadRequest(errorResponse);
        }

        [HttpPost]
        [Authorize]
        [Route("AddToCartUser")]
        public async Task<IActionResult> AddToCartUser([FromBody] AddToUserCartDto addToCartDto)
        {
            int variantId = addToCartDto.VariantId, quantity = addToCartDto.Quantity;

            _logger.LogInformation("AddToCartUser endpoint called - VariantId: {VariantId}, Quantity: {Quantity}", variantId, quantity);

            quantity = quantity <= 0 ? 1 : quantity;

            int userId = int.Parse(GetUserId());
            _logger.LogDebug("User {UserId} attempting to add item {variantId} to cart", userId, variantId);

            var result = await _cartService.AddToUserCart(userId, variantId, quantity);

            if (result is not null && result.IsSuccess)
            {
                _logger.LogInformation("Successfully added item to cart for user {UserId}", userId);
                return Ok(result);
            }

            _logger.LogWarning("Failed to add item to cart for user {UserId}: {Message}", userId, result?.Message ?? "");

            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Failed to add item to cart.",
                Path = "api/Carts/AddToCartUser",
                Method = HttpMethods.Post,
                TraceId = HttpContext.TraceIdentifier
            };

            return BadRequest(errorResponse);
        }

        [HttpPut]
        [Authorize]
        [Route("IncreaseUserAmount")]
        public async Task<IActionResult> IncreaseUserAmount([FromBody] UserCartOperationDto cartOperationDto)
        {
            int variantId = cartOperationDto.VariantId;

            _logger.LogInformation("IncreaseUserAmount endpoint called - VariantId: {VariantId}", variantId);

            int userId = int.Parse(GetUserId());
            _logger.LogDebug("User {UserId} attempting increase the amount of itme: {variantId}", userId, variantId);

            var result = await _cartService.IncreaseUserAmount(userId, variantId);

            if (result is not null && result.IsSuccess)
            {
                _logger.LogInformation("Successfully increased the amount for user {UserId}", userId);
                return Ok(result);
            }

            _logger.LogWarning("Failed to increase the amount for user {UserId}: {Message}", userId, result?.Message ?? "");

            int statusCode = result!.ErrorType.ToHttpStatusCode();

            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = statusCode,
                Message = result?.Message ?? "Failed to increase the amount.",
                Path = "api/Carts/IncreaseUserAmount",
                Method = HttpMethods.Put,
                TraceId = HttpContext.TraceIdentifier
            };

            return StatusCode(statusCode, errorResponse);
        }

        [HttpPut]
        [Authorize]
        [Route("DecreaseUserAmount")]
        public async Task<IActionResult> DecreaseUserAmount([FromBody] UserCartOperationDto cartOperationDto)
        {
            int variantId = cartOperationDto.VariantId;

            _logger.LogInformation("DecreaseUserAmount endpoint called - VariantId: {VariantId}", variantId);

            int userId = int.Parse(GetUserId());
            _logger.LogDebug("User {UserId} attempting decrease the amount of itme: {variantId}", userId, variantId);

            var result = await _cartService.DecreaseUserAmount(userId, variantId);

            if (result is not null && result.IsSuccess)
            {
                _logger.LogInformation("Successfully decreased the amount for user {UserId}", userId);
                return Ok(result);
            }

            _logger.LogWarning("Failed to decrease the amount for user {UserId}: {Message}", userId, result?.Message ?? "");

            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Failed to decrease the amount.",
                Path = "api/Carts/DecreaseUserAmount",
                Method = HttpMethods.Put,
                TraceId = HttpContext.TraceIdentifier
            };

            return BadRequest(errorResponse);
        }

        [HttpDelete]
        [Authorize]
        [Route("RemoveUserItem/{variantId}")]
        public async Task<IActionResult> RemoveUserItem(int variantId)
        {
            _logger.LogInformation("RemoveUserItem endpoint called - VariantId: {VariantId}", variantId);

            int userId = int.Parse(GetUserId());
            _logger.LogDebug("User {UserId} attempting remove itme: {variantId}", userId, variantId);

            var result = await _cartService.RemoveUserItem(userId, variantId);

            if (result is not null && result.IsSuccess)
            {
                _logger.LogInformation("Successfully removed the itme for user {UserId}", userId);
                return Ok(result);
            }

            _logger.LogWarning("Failed to remove itme for user {UserId}: {Message}", userId, result?.Message ?? "");

            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Failed to remove itme.",
                Path = "api/Carts/RemoveUserItem/{variantId}",
                Method = HttpMethods.Delete,
                TraceId = HttpContext.TraceIdentifier
            };

            return BadRequest(errorResponse);
        }

        [HttpGet]
        [Route("GetGuestCart/{guestId}")]
        public async Task<IActionResult> GetGuestCart(string guestId)
        {
            string currentLang = GetLangCode();
            string baseURL = BaseURL();

            _logger.LogDebug("Getting the cart of gueset: {0}", guestId);

            var result = await _cartService.GetGuestCart(guestId, currentLang, baseURL);

            if (result is not null && result.IsSuccess)
            {
                _logger.LogInformation("convert the guestCart to cartDto for user {GuestId}", guestId);

                var cartDto = result.Data;

                _logger.LogInformation("Successfully gettign the cart for gueset {GuestId}", guestId);
                return Ok(new ApiResponse<IEnumerable<CartDto>>()
                {
                    IsSuccess = true,
                    Message = "Successfully gettign the cart",
                    Data = cartDto
                });
            }

            _logger.LogWarning("Failed to get the cart for gueset {GuestId}: {Message}", guestId, result?.Message ?? "");

            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Failed to get the cart.",
                Path = "api/Carts/GetGuestCart/{guestId}",
                Method = HttpMethods.Get,
                TraceId = HttpContext.TraceIdentifier
            };

            return BadRequest(errorResponse);
        }

        [HttpPost]
        [Route("AddToGuestCart")]
        public async Task<IActionResult> AddToGuestCart([FromBody] AddToGuestCartDto addToGuestCartDto)
        {
            string guestId = addToGuestCartDto.GuestId;
            int variantId = addToGuestCartDto.VariantId, quantity = addToGuestCartDto.Quantity;

            _logger.LogInformation("AddToGuestCart endpoint called - VariantId: {VariantId}, Quantity: {Quantity}", variantId, quantity);

            quantity = quantity <= 0 ? 1 : quantity;

            _logger.LogDebug("Guest {GuestId} attempting to add item {variantId} to cart", guestId, variantId);

            var result = await _cartService.AddToGuestCart(guestId, variantId, quantity);

            if (result is not null && result.IsSuccess)
            {
                _logger.LogInformation("Successfully added item to cart for guest {GuestId}", guestId);
                return Ok(result);
            }

            _logger.LogWarning("Failed to add item to cart for guest {GuestId}: {Message}", guestId, result?.Message ?? "");

            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Failed to add item to cart.",
                Path = "api/Carts/AddToGuestCart",
                Method = HttpMethods.Post,
                TraceId = HttpContext.TraceIdentifier
            };

            return BadRequest(errorResponse);
        }

        [HttpPut]
        [Route("IncreaseGuestAmount")]
        public async Task<IActionResult> IncreaseGuestAmount([FromBody] GuestCartOperationDto guestCartOperationDto)
        {
            string guestId = guestCartOperationDto.GuestId;
            int variantId = guestCartOperationDto.VariantId;

            _logger.LogInformation("IncreaseGuestAmount endpoint called - VariantId: {VariantId}", variantId);
            _logger.LogDebug("Guest {GuestId} attempting increase the amount of itme: {variantId}", guestId, variantId);

            var result = await _cartService.IncreaseGuestAmount(guestId, variantId);

            if (result is not null && result.IsSuccess)
            {
                _logger.LogInformation("Successfully increased the amount for guest {GuestId}", guestId);
                return Ok(result);
            }

            _logger.LogWarning("Failed to increase the amount for guest {GuestId}: {Message}", guestId, result?.Message ?? "");

            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Failed to increase the amount.",
                Path = "api/Carts/IncreaseGuestAmount",
                Method = HttpMethods.Put,
                TraceId = HttpContext.TraceIdentifier
            };

            return BadRequest(errorResponse);
        }

        [HttpPut]
        [Route("DecreaseGuestAmount")]
        public async Task<IActionResult> DecreaseGuestAmount([FromBody] GuestCartOperationDto guestCartOperationDto)
        {
            string guestId = guestCartOperationDto.GuestId;
            int variantId = guestCartOperationDto.VariantId;

            _logger.LogInformation("DecreaseGuestAmount endpoint called - VariantId: {VariantId}", variantId);
            _logger.LogDebug("Guest {GuestId} attempting decrease the amount of itme: {variantId}", guestId, variantId);

            var result = await _cartService.DecreaseGuestAmount(guestId, variantId);

            if (result is not null && result.IsSuccess)
            {
                _logger.LogInformation("Successfully decreased the amount for guestId {GuestId}", guestId);
                return Ok(result);
            }

            _logger.LogWarning("Failed to decrease the amount for guestId {GuestId}: {Message}", guestId, result?.Message ?? "");

            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Failed to decrease the amount.",
                Path = "api/Carts/DecreaseGuestAmount",
                Method = HttpMethods.Put,
                TraceId = HttpContext.TraceIdentifier
            };

            return BadRequest(errorResponse);
        }

        [HttpDelete]
        [Route("RemoveGuestItem/{guestId}/{variantId}")]
        public async Task<IActionResult> RemoveGuestItem(string guestId, int variantId)
        {
            _logger.LogInformation("RemoveGuestItem endpoint called - VariantId: {VariantId}", variantId);
            _logger.LogDebug("Guest {GuestId} attempting remove the itme: {variantId}", guestId, variantId);

            var result = await _cartService.RemoveGuestItem(guestId, variantId);

            if (result is not null && result.IsSuccess)
            {
                _logger.LogInformation("Successfully remove the item for guestId {GuestId}", guestId);
                return Ok(result);
            }

            _logger.LogWarning("Failed to remove the item for guestId {GuestId}: {Message}", guestId, result?.Message ?? "");

            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Failed to remove the item.",
                Path = "api/Carts/RemoveGuestItem/{guestId}/{variantId}",
                Method = HttpMethods.Delete,
                TraceId = HttpContext.TraceIdentifier
            };

            return BadRequest(errorResponse);
        }

        [HttpPost]
        [Authorize]
        [Route("Merge")]
        public async Task<IActionResult> Merge([FromBody] MergeCartDto mergeCartDto)
        {
            string guestId = mergeCartDto.GuestId;

            _logger.LogInformation("Merge endpoint called");

            int userId = int.Parse(GetUserId());
            _logger.LogDebug("Merge the cart of user: {0}", userId);

            var result = await _cartService.Merge(userId, guestId);

            if (result is not null && result.IsSuccess)
            {
                _logger.LogInformation("Successfully merging the cart for {UserId}", userId);
                return Ok(result);
            }

            _logger.LogWarning("Failed to Merge {GuestId} and {UserId}: {Message}", guestId, userId, result?.Message ?? "");

            var errorResponse = new ApiErrorResponse
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = result?.Message ?? "Failed to Merge.",
                Path = "api/Carts/Merge",
                Method = HttpMethods.Post,
                TraceId = HttpContext.TraceIdentifier
            };

            return BadRequest(errorResponse);
        }
    }
}
