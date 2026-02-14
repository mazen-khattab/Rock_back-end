using Application.DTOs;
using Application.Interfaces;
using Application.Mapping;
using Application.Responses;
using Core.Entities;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public class CartService : ICartService
    {
        private readonly IServices<UserCart> _userCartService;
        private readonly IServices<GuestCart> _guestCartService;
        private readonly IServices<Variant> _variantService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CartService> _logger;

        public CartService(IServices<UserCart> userCartService, IServices<GuestCart> guestCartService, IServices<Variant> variantService, IUnitOfWork unitOfWork, ILogger<CartService> logger)
        {
            _userCartService = userCartService;
            _guestCartService = guestCartService;
            _variantService = variantService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }


        // user methods
        public async Task<ApiResponse<IEnumerable<CartDto>>> GetUserCart(int userId, int langId)
        {
            _logger.LogInformation("Retrieving cart for user {UserId}", userId);

            try
            {
                var cartItems = _userCartService.Query(c => c.UserId == userId)
                    .Include(c => c.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.Translations.Where(t => t.LanguageId == langId))
                    .Include(c => c.Variant)
                        .ThenInclude(v => v.Color)
                            .ThenInclude(c => c.Translations.Where(t => t.LanguageId == langId))
                    .Include(c => c.Variant)
                        .ThenInclude(v => v.Size)
                    .Include(c => c.Variant)
                        .ThenInclude(v => v.Images)
                            .ThenInclude(vi => vi.MediaAsset)
                            .ToList();

                if (!cartItems.Any())
                {
                    _logger.LogInformation("Cart is empty for user {UserId}", userId);
                    return new ApiResponse<IEnumerable<CartDto>>()
                    {
                        Success = true,
                        Message = "Cart is empty",
                        Data = Enumerable.Empty<CartDto>()
                    };
                }

                var cartDtos = cartItems.Select(cart => new CartDto()
                {
                    Id = cart.Id,
                    VariantId = cart.VariantId,
                    ProductId = cart.Variant.ProductId,
                    Name = cart.Variant?.Product?.Translations?.FirstOrDefault()?.Name ?? string.Empty,
                    Description = cart.Variant?.Product?.Translations?.FirstOrDefault()?.Description ?? string.Empty,
                    Price = cart.Variant?.Product.Price ?? 0,
                    imagesDtos = Mapper.MapToDto(cart.Variant?.Images.FirstOrDefault()),
                    Color = cart.Variant?.Color?.Translations?.FirstOrDefault()?.Name ?? string.Empty,
                    HexCode = cart.Variant?.Color.HexCode ?? string.Empty,
                    Size = cart.Variant?.Size?.Name ?? string.Empty,
                    Reserved = cart.Variant?.Reserved ?? 0,
                    Quantity = cart.Quantity,
                }).ToList();

                _logger.LogInformation("Successfully retrieved cart for user {UserId} with {ItemCount} items", userId, cartDtos.Count);

                return new ApiResponse<IEnumerable<CartDto>>()
                {
                    Success = true,
                    Message = "Cart retrieved successfully",
                    Data = cartDtos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cart for user {UserId}", userId);
                return new ApiResponse<IEnumerable<CartDto>>()
                {
                    Success = false,
                    Message = "An error occurred while retrieving cart"
                };
            }
        }

        public async Task<ApiResponse<string>> AddToUserCart(int userId, int variantId, int quantity)
        {
            _logger.LogInformation("Adding to user cart - UserId: {UserId}, VariantId: {VariantId}, Quantity: {Quantity}", userId, variantId, quantity);

            try
            {
                await _unitOfWork.BeginTransactionAsync();
                _logger.LogDebug("Transaction started for user {UserId}", userId);

                Variant? variant = await _variantService.GetByIdAsync(variantId);

                if (variant is null)
                {
                    _logger.LogWarning("Variant {VariantId} not found for user {UserId}", variantId, userId);
                    await _unitOfWork.CommitAsync();

                    return new ApiResponse<string>()
                    {
                        Success = false,
                        Message = "variant not found!"
                    };
                }

                _logger.LogDebug("Variant {VariantId} found. Available: {Available}, Reserved: {Reserved}", variantId, variant.Quantity, variant.Reserved);

                if (quantity + variant.Reserved > variant.Quantity)
                {
                    _logger.LogWarning("Insufficient stock for variant {VariantId}. Requested: {Requested}, Available: {Available}, Reserved: {Reserved}",
                        variantId, quantity, variant.Quantity, variant.Reserved);
                    await _unitOfWork.CommitAsync();

                    return new ApiResponse<string>()
                    {
                        Success = false,
                        Message = "No enough items!"
                    };
                }

                variant.Reserved += quantity;
                _logger.LogDebug("Updated variant {VariantId} reserved count to {Reserved}", variantId, variant.Reserved);

                UserCart? existingCart = await _userCartService.GetAsync(c => c.UserId == userId && c.VariantId == variantId);

                if (existingCart is not null)
                {
                    existingCart.Quantity += quantity;
                    _logger.LogDebug("Updated existing cart item for user {UserId}, variant {VariantId} to quantity {Quantity}", userId, variantId, existingCart.Quantity);
                }
                else
                {
                    UserCart newCart = new()
                    {
                        UserId = userId,
                        VariantId = variantId,
                        Quantity = quantity,
                        CreatedAt = DateTime.Now,
                    };

                    await _userCartService.AddAsync(newCart);
                    _logger.LogDebug("Created new cart item for user {UserId}, variant {VariantId}, quantity {Quantity}", userId, variantId, quantity);
                }

                await _unitOfWork.CommitAsync();
                await _unitOfWork.SaveChanges();
                _logger.LogInformation("Successfully added item to cart for user {UserId}", userId);

                return new ApiResponse<string>()
                {
                    Success = true,
                    Message = "Item added to cart successfully",
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to cart for user {UserId}, variant {VariantId}, quantity {Quantity}", userId, variantId, quantity);
                await _unitOfWork.RollbackAsync();

                return new ApiResponse<string>()
                {
                    Success = false,
                    Message = "Something went wrong while adding item to cart",
                };
            }
        }

        public async Task<ApiResponse<string>> IncreaseUserAmount(int userId, int variantId)
        {
            _logger.LogInformation("Increasing quantity for user {UserId}, variant {VariantId}", userId, variantId);

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                UserCart? cart = await _userCartService.GetAsync(c => c.UserId == userId && c.VariantId == variantId);

                if (cart is null)
                {
                    _logger.LogWarning("Cart item not found for user {UserId}, variant {VariantId}", userId, variantId);
                    await _unitOfWork.CommitAsync();

                    return new ApiResponse<string>()
                    {
                        Success = false,
                        Message = "Cart item not found!"
                    };
                }

                Variant? variant = await _variantService.GetByIdAsync(variantId);

                if (variant is null)
                {
                    _logger.LogWarning("Variant {VariantId} not found", variantId);
                    await _unitOfWork.CommitAsync();

                    return new ApiResponse<string>()
                    {
                        Success = false,
                        Message = "Variant not found!"
                    };
                }

                if (variant.Reserved + 1 > variant.Quantity)
                {
                    _logger.LogWarning("Insufficient stock for variant {VariantId}", variantId);
                    await _unitOfWork.CommitAsync();

                    return new ApiResponse<string>()
                    {
                        Success = false,
                        Message = "No enough items!"
                    };
                }

                cart.Quantity += 1;
                variant.Reserved += 1;

                await _unitOfWork.CommitAsync();
                await _unitOfWork.SaveChanges();
                _logger.LogInformation("Successfully increased quantity for user {UserId}, variant {VariantId}", userId, variantId);

                return new ApiResponse<string>()
                {
                    Success = true,
                    Message = "Quantity increased successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error increasing quantity for user {UserId}, variant {VariantId}", userId, variantId);
                await _unitOfWork.RollbackAsync();

                return new ApiResponse<string>()
                {
                    Success = false,
                    Message = "Something went wrong while increasing quantity"
                };
            }
        }

        public async Task<ApiResponse<string>> DecreaseUserAmount(int userId, int variantId)
        {
            _logger.LogInformation("Decreasing quantity for user {UserId}, variant {VariantId}", userId, variantId);

            try
            {
                UserCart? cart = await _userCartService.GetAsync(c => c.UserId == userId && c.VariantId == variantId);

                if (cart is null)
                {
                    _logger.LogWarning("Cart item not found for user {UserId}, variant {VariantId}", userId, variantId);
                    return new ApiResponse<string>()
                    {
                        Success = false,
                        Message = "Cart item not found!"
                    };
                }

                if (cart.Quantity <= 1)
                {
                    _logger.LogWarning("Can not descrease quantity for user {userId}", userId);
                    return new ApiResponse<string>()
                    {
                        Success = false,
                        Message = "can not decrease the amount!"
                    };
                }

                Variant? variant = await _variantService.GetByIdAsync(variantId);

                if (variant is null)
                {
                    _logger.LogWarning("Variant {VariantId} not found", variantId);
                    return new ApiResponse<string>()
                    {
                        Success = false,
                        Message = "Variant not found!"
                    };
                }

                if (cart.Quantity > 1)
                {
                    cart.Quantity -= 1;
                    variant.Reserved -= 1;
                    _logger.LogDebug("Decreased quantity for user {UserId}, variant {VariantId} to {Quantity}", userId, variantId, cart.Quantity);
                }
                //else
                //{
                //    await _userCartService.DeleteAsync(cart);
                //    variant.Reserved -= 1;
                //    _logger.LogDebug("Removed cart item for user {UserId}, variant {VariantId} as quantity reached zero", userId, variantId);
                //}

                await _unitOfWork.SaveChanges();
                _logger.LogInformation("Successfully decreased quantity for user {UserId}, variant {VariantId}", userId, variantId);

                return new ApiResponse<string>()
                {
                    Success = true,
                    Message = "Quantity decreased successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decreasing quantity for user {UserId}, variant {VariantId}", userId, variantId);

                return new ApiResponse<string>()
                {
                    Success = false,
                    Message = "Something went wrong while decreasing quantity"
                };
            }
        }

        public async Task<ApiResponse<string>> RemoveUserItem(int userId, int variantId)
        {
            _logger.LogInformation("Removing cart item for user {UserId}, variant {VariantId}", userId, variantId);

            try
            {
                UserCart? cart = await _userCartService.GetAsync(c => c.UserId == userId && c.VariantId == variantId);

                if (cart is null)
                {
                    _logger.LogWarning("Cart item not found for user {UserId}, variant {VariantId}", userId, variantId);
                    return new ApiResponse<string>()
                    {
                        Success = false,
                        Message = "Cart item not found!"
                    };
                }

                Variant? variant = await _variantService.GetByIdAsync(variantId);

                if (variant is not null)
                {
                    variant.Reserved -= cart.Quantity;
                    _logger.LogDebug("Updated variant {VariantId} reserved count after removal", variantId);
                }

                await _userCartService.DeleteAsync(cart);
                _logger.LogDebug("Deleted cart item for user {UserId}, variant {VariantId}", userId, variantId);

                await _unitOfWork.SaveChanges();
                _logger.LogInformation("Successfully removed cart item for user {UserId}, variant {VariantId}", userId, variantId);

                return new ApiResponse<string>()
                {
                    Success = true,
                    Message = "Item removed from cart successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cart item for user {UserId}, variant {VariantId}", userId, variantId);

                return new ApiResponse<string>()
                {
                    Success = false,
                    Message = "Something went wrong while removing item"
                };
            }
        }



        // guest methods
        public async Task<ApiResponse<IEnumerable<CartDto>>> GetGuestCart(string guestId, int langId)
        {
            _logger.LogInformation("Retrieving cart for guest {GuestId}", guestId);

            try
            {
                var cartItems = _guestCartService.Query(c => c.GuestId == guestId)
                    .Include(c => c.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.Translations.Where(t => t.LanguageId == langId))
                    .Include(c => c.Variant)
                        .ThenInclude(v => v.Color)
                            .ThenInclude(c => c.Translations.Where(t => t.LanguageId == langId))
                    .Include(c => c.Variant)
                        .ThenInclude(v => v.Size)
                    .Include(c => c.Variant)
                        .ThenInclude(v => v.Images)
                            .ThenInclude(vi => vi.MediaAsset)
                            .ToList();

                if (!cartItems.Any())
                {
                    _logger.LogInformation("Cart is empty or expired for guest {GuestId}", guestId);
                    return new ApiResponse<IEnumerable<CartDto>>()
                    {
                        Success = true,
                        Message = "Cart is empty or expired",
                        Data = Enumerable.Empty<CartDto>()
                    };
                }

                var cartDtos = cartItems.Select(cart => new CartDto()
                {
                    Id = cart.Id,
                    VariantId = cart.VariantId,
                    ProductId = cart.Variant.ProductId,
                    Name = cart.Variant?.Product?.Translations?.FirstOrDefault()?.Name ?? string.Empty,
                    Description = cart.Variant?.Product?.Translations?.FirstOrDefault()?.Description ?? string.Empty,
                    Price = cart.Variant?.Product.Price ?? 0,
                    imagesDtos = Mapper.MapToDto(cart.Variant?.Images.FirstOrDefault()),
                    Color = cart.Variant?.Color?.Translations?.FirstOrDefault()?.Name ?? string.Empty,
                    HexCode = cart.Variant?.Color.HexCode ?? string.Empty,
                    Size = cart.Variant?.Size?.Name ?? string.Empty,
                    Reserved = cart.Variant?.Reserved ?? 0,
                    Quantity = cart.Quantity,
                }).ToList();

                _logger.LogInformation("Successfully retrieved cart for guest {GuestId} with {ItemCount} items", guestId, cartDtos.Count);

                return new ApiResponse<IEnumerable<CartDto>>()
                {
                    Success = true,
                    Message = "Cart retrieved successfully",
                    Data = cartDtos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cart for guest {GuestId}", guestId);
                return new ApiResponse<IEnumerable<CartDto>>()
                {
                    Success = false,
                    Message = "An error occurred while retrieving cart"
                };
            }
        }

        public async Task<ApiResponse<string>> AddToGuestCart(string guestId, int variantId, int quantity)
        {
            _logger.LogInformation("Adding to guest cart - GuestId: {GuestId}, VariantId: {VariantId}, Quantity: {Quantity}", guestId, variantId, quantity);

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                Variant? variant = await _variantService.GetByIdAsync(variantId);

                if (variant is null)
                {
                    _logger.LogWarning("Variant {VariantId} not found for guest {GuestId}", variantId, guestId);
                    await _unitOfWork.CommitAsync();

                    return new ApiResponse<string>()
                    {
                        Success = false,
                        Message = "Variant not found!"
                    };
                }

                if (quantity + variant.Reserved > variant.Quantity)
                {
                    _logger.LogWarning("Insufficient stock for variant {VariantId}", variantId);
                    await _unitOfWork.CommitAsync();

                    return new ApiResponse<string>()
                    {
                        Success = false,
                        Message = "No enough items!"
                    };
                }

                variant.Reserved += quantity;

                GuestCart? existingCart = await _guestCartService.GetAsync(c => c.GuestId == guestId && c.VariantId == variantId);

                if (existingCart is not null)
                {
                    existingCart.Quantity += quantity;
                    existingCart.ExpireAt = DateTime.Now.AddHours(24);
                    _logger.LogDebug("Updated existing guest cart item for guest {GuestId}, variant {VariantId} to quantity {Quantity}", guestId, variantId, existingCart.Quantity);
                }
                else
                {
                    GuestCart newCart = new()
                    {
                        GuestId = guestId,
                        VariantId = variantId,
                        Quantity = quantity,
                        ExpireAt = DateTime.Now.AddHours(24)
                    };

                    await _guestCartService.AddAsync(newCart);
                    _logger.LogDebug("Created new guest cart item for guest {GuestId}, variant {VariantId}, quantity {Quantity}", guestId, variantId, quantity);
                }

                await _unitOfWork.CommitAsync();
                await _unitOfWork.SaveChanges();
                _logger.LogInformation("Successfully added item to guest cart for guest {GuestId}", guestId);

                return new ApiResponse<string>()
                {
                    Success = true,
                    Message = "Item added to cart successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to guest cart for guest {GuestId}, variant {VariantId}", guestId, variantId);
                await _unitOfWork.RollbackAsync();

                return new ApiResponse<string>()
                {
                    Success = false,
                    Message = "Something went wrong while adding item to cart"
                };
            }
        }

        public async Task<ApiResponse<string>> IncreaseGuestAmount(string guestId, int variantId)
        {
            _logger.LogInformation("Increasing quantity for guest {GuestId}, variant {VariantId}", guestId, variantId);

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                GuestCart? cart = await _guestCartService.GetAsync(c => c.GuestId == guestId && c.VariantId == variantId);

                if (cart is null)
                {
                    _logger.LogWarning("Cart item not found for guest {GuestId}, variant {VariantId}", guestId, variantId);
                    await _unitOfWork.CommitAsync();

                    return new ApiResponse<string>()
                    {
                        Success = false,
                        Message = "Cart item not found!"
                    };
                }

                Variant? variant = await _variantService.GetByIdAsync(variantId);

                if (variant is null)
                {
                    _logger.LogWarning("Variant {VariantId} not found", variantId);
                    await _unitOfWork.CommitAsync();

                    return new ApiResponse<string>()
                    {
                        Success = false,
                        Message = "Variant not found!"
                    };
                }

                if (variant.Reserved + 1 > variant.Quantity)
                {
                    _logger.LogWarning("Insufficient stock for variant {VariantId}", variantId);
                    await _unitOfWork.CommitAsync();

                    return new ApiResponse<string>()
                    {
                        Success = false,
                        Message = "No enough items!"
                    };
                }

                cart.Quantity += 1;
                cart.ExpireAt = DateTime.Now.AddHours(24);
                variant.Reserved += 1;

                await _unitOfWork.CommitAsync();
                await _unitOfWork.SaveChanges();
                _logger.LogInformation("Successfully increased quantity for guest {GuestId}, variant {VariantId}", guestId, variantId);

                return new ApiResponse<string>()
                {
                    Success = true,
                    Message = "Quantity increased successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error increasing quantity for guest {GuestId}, variant {VariantId}", guestId, variantId);
                await _unitOfWork.RollbackAsync();

                return new ApiResponse<string>()
                {
                    Success = false,
                    Message = "Something went wrong while increasing quantity"
                };
            }
        }

        public async Task<ApiResponse<string>> DecreaseGuestAmount(string guestId, int variantId)
        {
            _logger.LogInformation("Decreasing quantity for guest {GuestId}, variant {VariantId}", guestId, variantId);

            try
            {
                GuestCart? cart = await _guestCartService.GetAsync(c => c.GuestId == guestId && c.VariantId == variantId);

                if (cart is null)
                {
                    _logger.LogWarning("Cart item not found for guest {GuestId}, variant {VariantId}", guestId, variantId);
                    return new ApiResponse<string>()
                    {
                        Success = false,
                        Message = "Cart item not found!"
                    };
                }

                if (cart.Quantity <= 1)
                {
                    _logger.LogWarning("Can not descrease quantity for guest {GuestId}", guestId);
                    return new ApiResponse<string>()
                    {
                        Success = false,
                        Message = "can not decrease the amount!"
                    };
                }

                Variant? variant = await _variantService.GetByIdAsync(variantId);

                if (variant is null)
                {
                    _logger.LogWarning("Variant {VariantId} not found", variantId);
                    return new ApiResponse<string>()
                    {
                        Success = false,
                        Message = "Variant not found!"
                    };
                }

                if (cart.Quantity > 1)
                {
                    cart.Quantity -= 1;
                    cart.ExpireAt = DateTime.Now.AddHours(24);
                    variant.Reserved -= 1;
                    _logger.LogDebug("Decreased quantity for guest {GuestId}, variant {VariantId} to {Quantity}", guestId, variantId, cart.Quantity);
                }
                //else
                //{
                //    await _guestCartService.DeleteAsync(cart);
                //    variant.Reserved -= 1;
                //    _logger.LogDebug("Removed cart item for guest {GuestId}, variant {VariantId} as quantity reached zero", guestId, variantId);
                //}
                
                await _unitOfWork.SaveChanges();
                _logger.LogInformation("Successfully decreased quantity for guest {GuestId}, variant {VariantId}", guestId, variantId);

                return new ApiResponse<string>()
                {
                    Success = true,
                    Message = "Quantity decreased successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decreasing quantity for guest {GuestId}, variant {VariantId}", guestId, variantId);

                return new ApiResponse<string>()
                {
                    Success = false,
                    Message = "Something went wrong while decreasing quantity"
                };
            }
        }

        public async Task<ApiResponse<string>> RemoveGuestItem(string guestId, int variantId)
        {
            _logger.LogInformation("Removing cart item for guest {GuestId}, variant {VariantId}", guestId, variantId);

            try
            {
                GuestCart? cart = await _guestCartService.GetAsync(c => c.GuestId == guestId && c.VariantId == variantId);

                if (cart is null)
                {
                    _logger.LogWarning("Cart item not found for guest {GuestId}, variant {VariantId}", guestId, variantId);
                    return new ApiResponse<string>()
                    {
                        Success = false,
                        Message = "Cart item not found!"
                    };
                }

                Variant? variant = await _variantService.GetByIdAsync(variantId);

                if (variant is not null)
                {
                    variant.Reserved -= cart.Quantity;
                    _logger.LogDebug("Updated variant {VariantId} reserved count after removal", variantId);
                }

                await _guestCartService.DeleteAsync(cart);
                _logger.LogDebug("Deleted cart item for guest {GuestId}, variant {VariantId}", guestId, variantId);

                await _unitOfWork.SaveChanges();
                _logger.LogInformation("Successfully removed cart item for guest {GuestId}, variant {VariantId}", guestId, variantId);

                return new ApiResponse<string>()
                {
                    Success = true,
                    Message = "Item removed from cart successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cart item for guest {GuestId}, variant {VariantId}", guestId, variantId);

                return new ApiResponse<string>()
                {
                    Success = false,
                    Message = "Something went wrong while removing item"
                };
            }
        }



        public async Task<ApiResponse<IEnumerable<CartDto>>> Merge(int userId, string guestId)
        {
            _logger.LogInformation("Merging guest cart for guest {GuestId} to user {userId}", guestId, userId);

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var guestCartItems = await _guestCartService.GetAllAsync(
                    filter: c => c.GuestId == guestId,
                    tracked: true,
                    includes: c => c.Variant);

                if (!guestCartItems.Any())
                {
                    _logger.LogInformation("Guest cart is empty for guest {GuestId}", guestId);
                    await _unitOfWork.CommitAsync();

                    return new ApiResponse<IEnumerable<CartDto>>()
                    {
                        Success = true,
                        Message = "Guest cart is empty",
                        Data = Enumerable.Empty<CartDto>()
                    };
                }

                // add each item in guest cart to user cart
                foreach (var guestCart in guestCartItems)
                {
                    _logger.LogDebug("Processing guest cart item - VariantId: {VariantId}, Quantity: {Quantity}", guestCart.VariantId, guestCart.Quantity);

                    var userCart = await _userCartService.GetAsync(c => c.UserId == userId && c.VariantId == guestCart.VariantId);

                    // if the auth user has the current item in his cart, increase the amount, otherwise add it.
                    if (userCart is not null)
                    {
                        userCart.Quantity += guestCart.Quantity;
                        _logger.LogDebug("Updated existing user cart item for variant {VariantId} to quantity {Quantity}", guestCart.VariantId, userCart.Quantity);
                    }
                    else
                    {
                        UserCart newUserCart = new()
                        {
                            UserId = userId,
                            VariantId = guestCart.VariantId,
                            Quantity = guestCart.Quantity,
                            CreatedAt = DateTime.Now
                        };

                        await _userCartService.AddAsync(newUserCart);
                        _logger.LogDebug("Created new user cart item for variant {VariantId}, quantity {Quantity}", guestCart.VariantId, guestCart.Quantity);
                    }
                }

                // Delete all guest cart items
                foreach (var guestCart in guestCartItems)
                {
                    await _guestCartService.DeleteAsync(guestCart);
                    _logger.LogDebug("Deleted guest cart item for variant {VariantId}", guestCart.VariantId);
                }

                await _unitOfWork.CommitAsync();
                await _unitOfWork.SaveChanges();
                _logger.LogInformation("Successfully merged guest cart to user cart for guest {GuestId} with {ItemCount} items", guestId, guestCartItems.Count());

                return new ApiResponse<IEnumerable<CartDto>>()
                {
                    Success = true,
                    Message = "Cart merged successfully",
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error merging guest cart for guest {GuestId}", guestId);
                await _unitOfWork.RollbackAsync();

                return new ApiResponse<IEnumerable<CartDto>>()
                {
                    Success = false,
                    Message = "Something went wrong while merging cart"
                };
            }
        }
    }
}
