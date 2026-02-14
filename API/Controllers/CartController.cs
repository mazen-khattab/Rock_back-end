using Application.DTOs;
using Application.Interfaces;
using Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly ILogger<CartController> _logger;

        public CartController(ICartService cartService, ILogger<CartController> logger)
        {
            _cartService = cartService;
            _logger = logger;
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("Unauthorize user");
        }


        [HttpGet]
        [Authorize]
        [Route("GetUserCart/{langId}")]
        public async Task<IActionResult> GetUserCart(int langId)
        {
            try
            {
                int userId = int.Parse(GetUserId());
                _logger.LogDebug("Getting the cart of user: {0}", userId);

                var result = await _cartService.GetUserCart(userId, langId);

                if (!result.Success)
                {
                    _logger.LogWarning("Failed to get the cart of user {UserId}: {Message}", userId, result.Message);
                    return BadRequest(result.Message);
                }

                _logger.LogInformation("Successfully gettign the cart for user {UserId}", userId);
                return Ok(result);
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Exception in GetUserCart endpoint");
                return BadRequest(new
                {
                    Message = ex.Message,
                });
            }
        }

        [HttpPost]
        [Authorize]
        [Route("AddToCartUser")]
        public async Task<IActionResult> AddToCartUser([FromBody] AddToUserCartDto addToCartDto)
        {
            int variantId = addToCartDto.VariantId, quantity = addToCartDto.Quantity;

            _logger.LogInformation("AddToCartUser endpoint called - VariantId: {VariantId}, Quantity: {Quantity}", variantId, quantity);

            try
            {
                quantity = quantity <= 0 ? 1 : quantity;

                int userId = int.Parse(GetUserId());
                _logger.LogDebug("User {UserId} attempting to add item {variantId} to cart", userId, variantId);

                var result = await _cartService.AddToUserCart(userId, variantId, quantity);

                if (!result.Success)
                {
                    _logger.LogWarning("Failed to add item to cart for user {UserId}: {Message}", userId, result.Message);
                    return BadRequest(result.Message);
                }

                _logger.LogInformation("Successfully added item to cart for user {UserId}", userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in AddToCartUser endpoint");
                return BadRequest(new
                {
                    Message = ex.Message,
                });
            }
        }

        [HttpPost]
        [Authorize]
        [Route("IncreaseUserAmount")]
        public async Task<IActionResult> IncreaseUserAmount([FromBody] UserCartOperationDto cartOperationDto)
        {
            int variantId = cartOperationDto.VariantId;

            _logger.LogInformation("IncreaseUserAmount endpoint called - VariantId: {VariantId}", variantId);

            try
            {
                int userId = int.Parse(GetUserId());
                _logger.LogDebug("User {UserId} attempting increase the amount of itme: {variantId}", userId, variantId);

                var result = await _cartService.IncreaseUserAmount(userId, variantId);

                if (!result.Success)
                {
                    _logger.LogWarning("Failed to increase the amount for user {UserId}: {Message}", userId, result.Message);
                    return BadRequest(result.Message);
                }

                _logger.LogInformation("Successfully increased the amount for user {UserId}", userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in IncreaseUserAmount endpoint");
                return BadRequest(new
                {
                    Message = ex.Message,
                });
            }
        }

        [HttpPost]
        [Authorize]
        [Route("DecreaseUserAmount")]
        public async Task<IActionResult> DecreaseUserAmount([FromBody] UserCartOperationDto cartOperationDto)
        {
            int variantId = cartOperationDto.VariantId;

            _logger.LogInformation("DecreaseUserAmount endpoint called - VariantId: {VariantId}", variantId);

            try
            {
                int userId = int.Parse(GetUserId());
                _logger.LogDebug("User {UserId} attempting decrease the amount of itme: {variantId}", userId, variantId);

                var result = await _cartService.DecreaseUserAmount(userId, variantId);

                if (!result.Success)
                {
                    _logger.LogWarning("Failed to decrease the amount for user {UserId}: {Message}", userId, result.Message);
                    return BadRequest(result.Message);
                }

                _logger.LogInformation("Successfully decreased the amount for user {UserId}", userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in DecreaseUserAmount endpoint");
                return BadRequest(new
                {
                    Message = ex.Message,
                });
            }
        }

        [HttpPost]
        [Authorize]
        [Route("RemoveUserItem")]
        public async Task<IActionResult> RemoveUserItem([FromBody] UserCartOperationDto cartOperationDto)
        {
            int variantId = cartOperationDto.VariantId;

            _logger.LogInformation("RemoveUserItem endpoint called - VariantId: {VariantId}", variantId);

            try
            {
                int userId = int.Parse(GetUserId());
                _logger.LogDebug("User {UserId} attempting remove itme: {variantId}", userId, variantId);

                var result = await _cartService.RemoveUserItem(userId, variantId);

                if (!result.Success)
                {
                    _logger.LogWarning("Failed to remove itme for user {UserId}: {Message}", userId, result.Message);
                    return BadRequest(result.Message);
                }

                _logger.LogInformation("Successfully removed the itme for user {UserId}", userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in RemoveUserItem endpoint");
                return BadRequest(new
                {
                    Message = ex.Message,
                });
            }
        }

        [HttpGet]
        [Route("GetGuestCart/{langId}/{guestId}")]
        public async Task<IActionResult> GetGuestCart(string guestId, int langId)
        {
            try
            {
                _logger.LogDebug("Getting the cart of gueset: {0}", guestId);

                var result = await _cartService.GetGuestCart(guestId, langId);

                if (!result.Success)
                {
                    _logger.LogWarning("Failed to get the cart of gueset {GuestId}: {Message}", guestId, result.Message);
                    return BadRequest(result.Message);
                }

                _logger.LogInformation("Successfully gettign the cart for gueset {GuestId}", guestId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetGuestCart endpoint");
                return BadRequest(new
                {
                    Message = ex.Message,
                });
            }
        }

        [HttpPost]
        [Route("AddToGuestCart")]
        public async Task<IActionResult> AddToGuestCart([FromBody] AddToGuestCartDto addToGuestCartDto)
        {
            string guestId = addToGuestCartDto.GuestId;
            int variantId = addToGuestCartDto.VariantId, quantity = addToGuestCartDto.Quantity;

            _logger.LogInformation("AddToGuestCart endpoint called - VariantId: {VariantId}, Quantity: {Quantity}", variantId, quantity);

            try
            {
                quantity = quantity <= 0 ? 1 : quantity;

                _logger.LogDebug("Guest {GuestId} attempting to add item {variantId} to cart", guestId, variantId);

                var result = await _cartService.AddToGuestCart(guestId, variantId, quantity);

                if (!result.Success)
                {
                    _logger.LogWarning("Failed to add item to cart for guest {GuestId}: {Message}", guestId, result.Message);
                    return BadRequest(result.Message);
                }

                _logger.LogInformation("Successfully added item to cart for guest {GuestId}", guestId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in AddToGuestCart endpoint");
                return BadRequest(new
                {
                    Message = ex.Message,
                });
            }
        }

        [HttpPost]
        [Route("IncreaseGuestAmount")]
        public async Task<IActionResult> IncreaseGuestAmount([FromBody] GuestCartOperationDto guestCartOperationDto)
        {
            string guestId = guestCartOperationDto.GuestId;
            int variantId = guestCartOperationDto.VariantId;

            _logger.LogInformation("IncreaseGuestAmount endpoint called - VariantId: {VariantId}", variantId);

            try
            {
                _logger.LogDebug("Guest {GuestId} attempting increase the amount of itme: {variantId}", guestId, variantId);

                var result = await _cartService.IncreaseGuestAmount(guestId, variantId);

                if (!result.Success)
                {
                    _logger.LogWarning("Failed to increase the amount for guest {GuestId}: {Message}", guestId, result.Message);
                    return BadRequest(result.Message);
                }

                _logger.LogInformation("Successfully increased the amount for guest {GuestId}", guestId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in IncreaseGuestAmount endpoint");
                return BadRequest(new
                {
                    Message = ex.Message,
                });
            }
        }

        [HttpPost]
        [Route("DecreaseGuestAmount")]
        public async Task<IActionResult> DecreaseGuestAmount([FromBody] GuestCartOperationDto guestCartOperationDto)
        {
            string guestId = guestCartOperationDto.GuestId;
            int variantId = guestCartOperationDto.VariantId;

            _logger.LogInformation("DecreaseGuestAmount endpoint called - VariantId: {VariantId}", variantId);

            try
            {
                _logger.LogDebug("Guest {GuestId} attempting decrease the amount of itme: {variantId}", guestId, variantId);

                var result = await _cartService.DecreaseGuestAmount(guestId, variantId);

                if (!result.Success)
                {
                    _logger.LogWarning("Failed to decrease the amount for guestId {GuestId}: {Message}", guestId, result.Message);
                    return BadRequest(result.Message);
                }

                _logger.LogInformation("Successfully decreased the amount for guestId {GuestId}", guestId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in DecreaseGuestAmount endpoint");
                return BadRequest(new
                {
                    Message = ex.Message,
                });
            }
        }

        [HttpPost]
        [Authorize]
        [Route("Merge")]
        public async Task<IActionResult> Merge([FromBody] MergeCartDto mergeCartDto)
        {
            string guestId = mergeCartDto.GuestId;

            _logger.LogInformation("Merge endpoint called");

            try
            {
                int userId = int.Parse(GetUserId());
                _logger.LogDebug("Merge the cart of user: {0}", userId);

                var result = await _cartService.Merge(userId, guestId);

                if (!result.Success)
                {
                    _logger.LogWarning("Failed to Merge {GuestId} and {UserId}: {Message}", guestId, userId, result.Message);
                    return BadRequest(result.Message);
                }

                _logger.LogInformation("Successfully merging the cart for {UserId}", userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in Merge endpoint");
                return BadRequest(new
                {
                    Message = ex.Message,
                });
            }
        }
    }
}
