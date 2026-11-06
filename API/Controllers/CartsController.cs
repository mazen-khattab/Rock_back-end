using API.Extensions;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet("GetUserCart")]
        [Authorize]
        public async Task<IActionResult> GetUserCart()
        {
            int userId = User.GetRequiredUserId();
            _logger.LogInformation("Cart requested for user ID: {UserId}", userId);
            return this.ToApiResult(await _cartService.GetUserCart(userId, Request.GetLanguageCode(), Request.GetBaseUrl()));
        }

        [HttpPost("AddToCartUser")]
        [Authorize]
        public async Task<IActionResult> AddToCartUser([FromBody] AddToUserCartDto addToCartDto)
        {
            int userId = User.GetRequiredUserId();
            _logger.LogInformation("Adding variant ID: {VariantId} to cart for user ID: {UserId}", addToCartDto.VariantId, userId);
            int quantity = addToCartDto.Quantity <= 0 ? 1 : addToCartDto.Quantity;
            return this.ToApiResult(await _cartService.AddToUserCart(userId, addToCartDto.VariantId, quantity));
        }

        [HttpPut("IncreaseUserAmount")]
        [Authorize]
        public async Task<IActionResult> IncreaseUserAmount([FromBody] UserCartOperationDto cartOperationDto)
        {
            int userId = User.GetRequiredUserId();
            _logger.LogInformation("Increasing variant ID: {VariantId} for user ID: {UserId}", cartOperationDto.VariantId, userId);
            return this.ToApiResult(await _cartService.IncreaseUserAmount(userId, cartOperationDto.VariantId));
        }

        [HttpPut("DecreaseUserAmount")]
        [Authorize]
        public async Task<IActionResult> DecreaseUserAmount([FromBody] UserCartOperationDto cartOperationDto)
        {
            int userId = User.GetRequiredUserId();
            _logger.LogInformation("Decreasing variant ID: {VariantId} for user ID: {UserId}", cartOperationDto.VariantId, userId);
            return this.ToApiResult(await _cartService.DecreaseUserAmount(userId, cartOperationDto.VariantId));
        }

        [HttpDelete("RemoveUserItem/{variantId}")]
        [Authorize]
        public async Task<IActionResult> RemoveUserItem(int variantId)
        {
            int userId = User.GetRequiredUserId();
            _logger.LogInformation("Removing variant ID: {VariantId} for user ID: {UserId}", variantId, userId);
            return this.ToApiResult(await _cartService.RemoveUserItem(userId, variantId));
        }

        [HttpGet("GetGuestCart/{guestId}")]
        public async Task<IActionResult> GetGuestCart(string guestId)
        {
            _logger.LogInformation("Guest cart requested for guest ID: {GuestId}", guestId);
            return this.ToApiResult(await _cartService.GetGuestCart(guestId, Request.GetLanguageCode(), Request.GetBaseUrl()));
        }

        [HttpPost("AddToGuestCart")]
        public async Task<IActionResult> AddToGuestCart([FromBody] AddToGuestCartDto addToGuestCartDto)
        {
            _logger.LogInformation("Adding variant ID: {VariantId} to guest cart", addToGuestCartDto.VariantId);
            int quantity = addToGuestCartDto.Quantity <= 0 ? 1 : addToGuestCartDto.Quantity;
            return this.ToApiResult(await _cartService.AddToGuestCart(addToGuestCartDto.GuestId, addToGuestCartDto.VariantId, quantity));
        }

        [HttpPut("IncreaseGuestAmount")]
        public async Task<IActionResult> IncreaseGuestAmount([FromBody] GuestCartOperationDto guestCartOperationDto)
        {
            _logger.LogInformation("Increasing guest cart variant ID: {VariantId}", guestCartOperationDto.VariantId);
            return this.ToApiResult(await _cartService.IncreaseGuestAmount(guestCartOperationDto.GuestId, guestCartOperationDto.VariantId));
        }

        [HttpPut("DecreaseGuestAmount")]
        public async Task<IActionResult> DecreaseGuestAmount([FromBody] GuestCartOperationDto guestCartOperationDto)
        {
            _logger.LogInformation("Decreasing guest cart variant ID: {VariantId}", guestCartOperationDto.VariantId);
            return this.ToApiResult(await _cartService.DecreaseGuestAmount(guestCartOperationDto.GuestId, guestCartOperationDto.VariantId));
        }

        [HttpDelete("RemoveGuestItem/{guestId}/{variantId}")]
        public async Task<IActionResult> RemoveGuestItem(string guestId, int variantId)
        {
            _logger.LogInformation("Removing guest cart variant ID: {VariantId}", variantId);
            return this.ToApiResult(await _cartService.RemoveGuestItem(guestId, variantId));
        }

        [HttpPost("Merge")]
        [Authorize]
        public async Task<IActionResult> Merge([FromBody] MergeCartDto mergeCartDto)
        {
            int userId = User.GetRequiredUserId();
            _logger.LogInformation("Cart merge requested for user ID: {UserId}", userId);
            return this.ToApiResult(await _cartService.Merge(userId, mergeCartDto.GuestId));
        }
    }
}
