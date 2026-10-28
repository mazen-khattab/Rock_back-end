using Application.DTOs;
using Application.Interfaces;
using Application.Mapping;
using Application.Responses;
using Core.Entities;
using Core.Enum;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace Application.Services
{
    public class CartService : ICartService
    {
        private readonly IServices<UserCart> _userCartService;
        private readonly IServices<GuestCart> _guestCartService;
        private readonly IServices<Variant> _variantService;
        private readonly IServices<Language> _langService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CartService> _logger;

        public CartService(IServices<UserCart> userCartService, IServices<GuestCart> guestCartService, IServices<Variant> variantService, IServices<Language> langService, IUnitOfWork unitOfWork, ILogger<CartService> logger)
        {
            _userCartService = userCartService;
            _guestCartService = guestCartService;
            _variantService = variantService;
            _langService = langService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }


        private async Task<int> GetLanguageId(string langCode)
        {
            _logger.LogInformation("Retrieving language ID for language code: {LangCode}", langCode);
            int langId = await _langService.Query(l => l.Code == langCode, false).Select(l => l.Id).FirstOrDefaultAsync();

            if (langId == 0)
            {
                _logger.LogWarning("Language code {LangCode} not found. Defaulting to language ID 1.", langCode);
                return 1; // Default to Arabic if not found
            }

            _logger.LogInformation("Found language ID {LangId} for language code {LangCode}", langId, langCode);
            return langId;
        }

        // user methods
        #region GetUserCart
        public async Task<IEnumerable<UserCart>> FetchCartWithDetailsAsync(int userId, int langId)
        {
            _logger.LogInformation("Fetching cart entities from DB for user {UserId} with Language ID {LangId}", userId, langId);

            return await _userCartService.Query(c => c.UserId == userId, false)
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
                .ToListAsync();
        }
        public async Task<ApiResponse<IEnumerable<CartDto>>> GetUserCart(int userId, string langCode, string baseUrl)
        {
            _logger.LogInformation("Retrieving cart for user {UserId}", userId);

            int langId = await GetLanguageId(langCode);
            var cartItems = await FetchCartWithDetailsAsync(userId, langId);

            if (cartItems.Any())
            {
                _logger.LogInformation("Successfully retrieved cart for user {UserId} with {ItemCount} items", userId, cartItems.Count());

                var cartDto = cartItems.ToDtoList(baseUrl) ?? new();

                return new ApiResponse<IEnumerable<CartDto>>()
                {
                    IsSuccess = true,
                    Message = "Cart retrieved successfully",
                    Data = cartDto
                };
            }

            _logger.LogInformation("Cart is empty for user {UserId}", userId);
            return new ApiResponse<IEnumerable<CartDto>>()
            {
                IsSuccess = true,
                Message = "Cart is empty",
                Data = Enumerable.Empty<CartDto>()
            };
        }
        #endregion

        #region AddToUserCart
        public async Task<ApiResponse<string>> AddToUserCart(int userId, int variantId, int quantity)
        {
            _logger.LogInformation("Adding to user cart - UserId: {UserId}, VariantId: {VariantId}, Quantity: {Quantity}", userId, variantId, quantity);

            try
            {
                _logger.LogDebug("Transaction started for user {UserId}", userId);

                Variant? variant = await _variantService.GetByIdAsync(variantId);

                if (variant is null)
                {
                    _logger.LogWarning("Variant {VariantId} not found for user {UserId}", variantId, userId);
                    return new ApiResponse<string>()
                    {
                        IsSuccess = false,
                        ErrorType = ErrorType.NotFound,
                        Message = "variant not found!"
                    };
                }

                _logger.LogDebug("Variant {VariantId} found. Available: {Available}, Reserved: {Reserved}", variantId, variant.Quantity, variant.Reserved);

                if (quantity + variant.Reserved > variant.Quantity)
                {
                    _logger.LogWarning("Insufficient stock for variant {VariantId}. Requested: {Requested}, Available: {Available}, Reserved: {Reserved}",
                        variantId, quantity, variant.Quantity, variant.Reserved);

                    return new ApiResponse<string>()
                    {
                        IsSuccess = false,
                        ErrorType = ErrorType.InsufficientStock,
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
                    };

                    _userCartService.Add(newCart);
                    _logger.LogDebug("Created new cart item for user {UserId}, variant {VariantId}, quantity {Quantity}", userId, variantId, quantity);
                }

                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Successfully added item to cart for user {UserId}", userId);

                return new ApiResponse<string>()
                {
                    IsSuccess = true,
                    Message = "Item added to cart successfully",
                };
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Error adding item to cart for user {UserId}, variant {VariantId}, quantity {Quantity}", userId, variantId, quantity);

                return new ApiResponse<string>()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.ConcurrencyConflict,
                    Message = "Concurrency_Conflict: The stock was just updated by another user."
                };
            }
        }
        #endregion

        #region IncreaseUserAmount
        public async Task<ApiResponse<string>> IncreaseUserAmount(int userId, int variantId)
        {
            _logger.LogInformation("Increasing quantity for user {UserId}, variant {VariantId}", userId, variantId);

            try
            {
                Variant? variant = await _variantService.GetByIdAsync(variantId);

                if (variant is null)
                {
                    _logger.LogWarning("Variant {VariantId} not found", variantId);

                    return new ApiResponse<string>()
                    {
                        IsSuccess = false,
                        ErrorType = ErrorType.NotFound,
                        Message = "Variant not found!"
                    };
                }

                if (variant.Reserved + 1 > variant.Quantity)
                {
                    _logger.LogWarning("Insufficient stock for variant {VariantId}", variantId);

                    return new ApiResponse<string>()
                    {
                        IsSuccess = false,
                        ErrorType = ErrorType.InsufficientStock,
                        Message = "No enough items!"
                    };
                }

                UserCart? cart = await _userCartService.GetAsync(c => c.UserId == userId && c.VariantId == variantId);

                if (cart is null)
                {
                    _logger.LogWarning("Cart item not found for user {UserId}, variant {VariantId}", userId, variantId);
                    return new ApiResponse<string>()
                    {
                        IsSuccess = false,
                        ErrorType = ErrorType.NotFound,
                        Message = "Cart item not found!"
                    };
                }

                cart.Quantity += 1;
                variant.Reserved += 1;

                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Successfully increased quantity for user {UserId}, variant {VariantId}", userId, variantId);

                return new ApiResponse<string>()
                {
                    IsSuccess = true,
                    Message = "Quantity increased successfully"
                };
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Error increasing quantity for user {UserId}, variant {VariantId}", userId, variantId);

                return new ApiResponse<string>()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.ConcurrencyConflict,
                    Message = "Concurrency_Conflict: The stock was just updated by another user."
                };
            }
        }
        #endregion

        #region DecreaseUserAmount
        public async Task<ApiResponse<string>> DecreaseUserAmount(int userId, int variantId)
        {
            _logger.LogInformation("Decreasing quantity for user {UserId}, variant {VariantId}", userId, variantId);

            Variant? variant = await _variantService.GetByIdAsync(variantId);

            if (variant is null)
            {
                _logger.LogWarning("Variant {VariantId} not found", variantId);
                return new ApiResponse<string>()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.NotFound,
                    Message = "Variant not found!"
                };
            }

            UserCart? cart = await _userCartService.GetAsync(c => c.UserId == userId && c.VariantId == variantId);

            if (cart is null)
            {
                _logger.LogWarning("Cart item not found for user {UserId}, variant {VariantId}", userId, variantId);
                return new ApiResponse<string>()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.NotFound,
                    Message = "Cart item not found!"
                };
            }

            int maxRetryAttempts = 3;

            for (int retry = 1; retry <= maxRetryAttempts; retry++)
            {
                try
                {
                    if (cart.Quantity <= 1)
                    {
                        _logger.LogWarning("Cannot decrease quantity for user {UserId}, variant {VariantId} as it is already at minimum", userId, variantId);
                        return new ApiResponse<string>()
                        {
                            IsSuccess = false,
                            ErrorType = ErrorType.InsufficientStock,
                            Message = "Cannot decrease the amount!"
                        };
                    }

                    cart.Quantity -= 1;
                    variant.Reserved -= 1;

                    await _unitOfWork.SaveChangesAsync();
                    _logger.LogInformation("Successfully decreased quantity for user {UserId}, variant {VariantId}", userId, variantId);

                    return new ApiResponse<string>()
                    {
                        IsSuccess = true,
                        Message = "Quantity decreased successfully"
                    };
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogWarning(ex, "Concurrency conflict while decreasing quantity for user {UserId}, variant {VariantId}. Attempt {Attempt}/{MaxAttempts}", userId, variantId, retry, maxRetryAttempts);

                    if (retry == maxRetryAttempts)
                    {
                        return new ApiResponse<string>()
                        {
                            IsSuccess = false,
                            ErrorType = ErrorType.ConcurrencyConflict,
                            Message = "Concurrency_Conflict: The stock was just updated by another user."
                        };
                    }
                    _logger.LogInformation("Concurrency conflict detected. Reloading entities and retrying... (Attempt {Retry})", retry);

                    // Refresh the entity to get the latest values from the database
                    await _unitOfWork.Entry(variant).ReloadAsync();
                    await _unitOfWork.Entry(cart).ReloadAsync();
                }
            }

            return new ApiResponse<string>()
            {
                IsSuccess = false,
                ErrorType = ErrorType.ServerError,
                Message = "Operation failed."
            };
        }
        #endregion

        #region RemoveUserItem
        public async Task<ApiResponse<string>> RemoveUserItem(int userId, int variantId)
        {
            _logger.LogInformation("Removing cart item for user {UserId}, variant {VariantId}", userId, variantId);

            // 1. Fetch the data outside the retry loop
            UserCart? cart = await _userCartService.GetAsync(c => c.UserId == userId && c.VariantId == variantId);

            if (cart is null)
            {
                _logger.LogWarning("Cart item not found for user {UserId}, variant {VariantId}", userId, variantId);
                return new ApiResponse<string>()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.NotFound,
                    Message = "Cart item not found!"
                };
            }

            Variant? variant = await _variantService.GetByIdAsync(variantId);

            int maxRetryAttempts = 3;

            for (int retry = 1; retry <= maxRetryAttempts; retry++)
            {
                try
                {
                    // 2. Perform calculations and deletion inside the loop so they apply to fresh data upon retry
                    if (variant is not null)
                    {
                        _logger.LogInformation("Trying to update variant reserved, the current reserved Qty: {Reserved}", variant.Reserved);
                        variant.Reserved -= cart.Quantity;
                        _logger.LogDebug("Decreased variant {VariantId} reserved count by {Quantity}", variantId, cart.Quantity);
                    }

                    _userCartService.Delete(cart);

                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation("Successfully removed cart item for user {UserId}, variant {VariantId}", userId, variantId);
                    return new ApiResponse<string>()
                    {
                        IsSuccess = true,
                        Message = "Item removed from cart successfully"
                    };
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogWarning(ex, "Concurrency conflict while removing item for user {UserId}, variant {VariantId}. Attempt {Attempt}/{MaxAttempts}", userId, variantId, retry, maxRetryAttempts);

                    if (retry == maxRetryAttempts)
                    {
                        return new ApiResponse<string>()
                        {
                            IsSuccess = false,
                            ErrorType = ErrorType.ConcurrencyConflict,
                            Message = "Concurrency_Conflict: The system is busy, please try again."
                        };
                    }

                    _logger.LogInformation("Concurrency conflict detected. Reloading entities and retrying... (Attempt {Retry})", retry);

                    // 3. Reload the Variant entity if it exists
                    if (variant is not null)
                    {
                        await _unitOfWork.Entry(variant).ReloadAsync();
                    }

                    // 4. Pro trick: Reload the Cart inside its own try-catch block
                    try
                    {
                        await _unitOfWork.Entry(cart).ReloadAsync();
                    }
                    catch (InvalidOperationException)
                    {
                        // The magic happens here: If this error is thrown, it means a double-click occurred 
                        // and the concurrent request already deleted the cart!
                        // We don't need to continue the loop or decrease the Variant quantity again; 
                        // we just return a success response immediately.
                        _logger.LogInformation("Cart item was already removed by a concurrent request. Returning success implicitly for user {UserId}.", userId);

                        return new ApiResponse<string>()
                        {
                            IsSuccess = true,
                            Message = "Item removed from cart successfully"
                        };
                    }
                }
            }

            return new ApiResponse<string>()
            {
                IsSuccess = false,
                ErrorType = ErrorType.ServerError,
                Message = "Operation failed."
            };
        }
        #endregion

        // guest methods
        #region GetGuestCart
        public async Task<IEnumerable<GuestCart>> FetchGuestCartWithDetailsAsync(string guestId, int langId)
        {
            _logger.LogInformation("Fetching guest cart entities from DB for guest {GuestId} with Language ID {LangId}", guestId, langId);

            return await _guestCartService.Query(c => c.GuestId == guestId)
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
                .ToListAsync();
        }
        public async Task<ApiResponse<IEnumerable<CartDto>>> GetGuestCart(string guestId, string langCode, string baseUrl)
        {
            _logger.LogInformation("Processing guest cart request for guest {GuestId} with language code {LangCode}", guestId, langCode);

            int langId = await GetLanguageId(langCode);
            var cartItems = await FetchGuestCartWithDetailsAsync(guestId, langId);

            if (cartItems.Any())
            {
                _logger.LogInformation("Successfully retrieved cart for guest {GuestId} with {ItemCount} items", guestId, cartItems.Count());

                var cartDto = cartItems.ToDtoList(baseUrl) ?? new();

                return new ApiResponse<IEnumerable<CartDto>>()
                {
                    IsSuccess = true,
                    Message = "Cart retrieved successfully",
                    Data = cartDto
                };
            }

            _logger.LogInformation("Cart is empty or expired for guest {GuestId}", guestId);
            return new ApiResponse<IEnumerable<CartDto>>()
            {
                IsSuccess = true,
                Message = "Cart is empty or expired",
                Data = Enumerable.Empty<CartDto>()
            };
        }
        #endregion

        #region AddToGuestCart
        public async Task<ApiResponse<string>> AddToGuestCart(string guestId, int variantId, int quantity)
        {
            _logger.LogInformation("Adding to guest cart - GuestId: {GuestId}, VariantId: {VariantId}, Quantity: {Quantity}", guestId, variantId, quantity);

            try
            {
                Variant? variant = await _variantService.GetByIdAsync(variantId);

                if (variant is null)
                {
                    _logger.LogWarning("Variant {VariantId} not found for guest {GuestId}", variantId, guestId);

                    return new ApiResponse<string>()
                    {
                        IsSuccess = false,
                        ErrorType = ErrorType.NotFound,
                        Message = "Variant not found!"
                    };
                }

                if (quantity + variant.Reserved > variant.Quantity)
                {
                    _logger.LogWarning("Insufficient stock for variant {VariantId}. Requested: {Requested}, Available: {Available}, Reserved: {Reserved}",
                        variantId, quantity, variant.Quantity, variant.Reserved);

                    return new ApiResponse<string>()
                    {
                        IsSuccess = false,
                        ErrorType = ErrorType.InsufficientStock,
                        Message = "No enough items!"
                    };
                }

                variant.Reserved += quantity;
                _logger.LogDebug("Updated variant {VariantId} reserved count to {Reserved}", variantId, variant.Reserved);

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

                    _guestCartService.Add(newCart);
                    _logger.LogDebug("Created new guest cart item for guest {GuestId}, variant {VariantId}, quantity {Quantity}", guestId, variantId, quantity);
                }

                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Successfully added item to guest cart for guest {GuestId}", guestId);

                return new ApiResponse<string>()
                {
                    IsSuccess = true,
                    Message = "Item added to cart successfully"
                };
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Error adding item to guest cart for guest {GuestId}, variant {VariantId}", guestId, variantId);

                return new ApiResponse<string>()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.ConcurrencyConflict,
                    Message = "Concurrency_Conflict: The stock was just updated by another user."
                };
            }
        }
        #endregion

        #region IncreaseGuestAmount
        public async Task<ApiResponse<string>> IncreaseGuestAmount(string guestId, int variantId)
        {
            _logger.LogInformation("Increasing quantity for guest {GuestId}, variant {VariantId}", guestId, variantId);

            try
            {
                Variant? variant = await _variantService.GetByIdAsync(variantId);

                if (variant is null)
                {
                    _logger.LogWarning("Variant {VariantId} not found", variantId);

                    return new ApiResponse<string>()
                    {
                        IsSuccess = false,
                        ErrorType = ErrorType.NotFound,
                        Message = "Variant not found!"
                    };
                }

                if (variant.Reserved + 1 > variant.Quantity)
                {
                    _logger.LogWarning("Insufficient stock for variant {VariantId}", variantId);

                    return new ApiResponse<string>()
                    {
                        IsSuccess = false,
                        ErrorType = ErrorType.InsufficientStock,
                        Message = "No enough items!"
                    };
                }

                GuestCart? cart = await _guestCartService.GetAsync(c => c.GuestId == guestId && c.VariantId == variantId);

                if (cart is null)
                {
                    _logger.LogWarning("Cart item not found for guest {GuestId}, variant {VariantId}", guestId, variantId);

                    return new ApiResponse<string>()
                    {
                        IsSuccess = false,
                        ErrorType = ErrorType.NotFound,
                        Message = "Cart item not found!"
                    };
                }

                cart.Quantity += 1;
                cart.ExpireAt = DateTime.Now.AddHours(24);
                variant.Reserved += 1;

                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Successfully increased quantity for guest {GuestId}, variant {VariantId}", guestId, variantId);

                return new ApiResponse<string>()
                {
                    IsSuccess = true,
                    Message = "Quantity increased successfully"
                };
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Error increasing quantity for guest {GuestId}, variant {VariantId}", guestId, variantId);

                return new ApiResponse<string>()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.ConcurrencyConflict,
                    Message = "Concurrency_Conflict: The stock was just updated by another user."
                };
            }
        }
        #endregion

        #region DecreaseGuestAmount
        public async Task<ApiResponse<string>> DecreaseGuestAmount(string guestId, int variantId)
        {
            _logger.LogInformation("Decreasing quantity for guest {GuestId}, variant {VariantId}", guestId, variantId);
            Variant? variant = await _variantService.GetByIdAsync(variantId);

            if (variant is null)
            {
                _logger.LogWarning("Variant {VariantId} not found", variantId);
                return new ApiResponse<string>()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.NotFound,
                    Message = "Variant not found!"
                };
            }

            GuestCart? cart = await _guestCartService.GetAsync(c => c.GuestId == guestId && c.VariantId == variantId);

            if (cart is null)
            {
                _logger.LogWarning("Cart item not found for guest {GuestId}, variant {VariantId}", guestId, variantId);
                return new ApiResponse<string>()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.NotFound,
                    Message = "Cart item not found!"
                };
            }

            int maxRetryAttempts = 3;

            for (int retry = 1; retry <= maxRetryAttempts; retry++)
            {
                try
                {
                    if (cart.Quantity <= 1)
                    {
                        _logger.LogWarning("Can not descrease quantity for guest {GuestId}", guestId);
                        return new ApiResponse<string>()
                        {
                            IsSuccess = false,
                            ErrorType = ErrorType.InsufficientStock,
                            Message = "can not decrease the amount!"
                        };
                    }

                    cart.Quantity -= 1;
                    cart.ExpireAt = DateTime.Now.AddHours(24);
                    variant.Reserved -= 1;
                    _logger.LogDebug("Decreased quantity for guest {GuestId}, variant {VariantId} to {Quantity}", guestId, variantId, cart.Quantity);

                    await _unitOfWork.SaveChangesAsync();
                    _logger.LogInformation("Successfully decreased quantity for guest {GuestId}, variant {VariantId}", guestId, variantId);

                    return new ApiResponse<string>()
                    {
                        IsSuccess = true,
                        Message = "Quantity decreased successfully"
                    };
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogWarning(ex, "Concurrency conflict while decreasing quantity for guest {GuestId}, variant {VariantId}. Attempt {Attempt}/{MaxAttempts}", guestId, variantId, retry, maxRetryAttempts);

                    if (retry == maxRetryAttempts)
                    {
                        return new ApiResponse<string>()
                        {
                            IsSuccess = false,
                            ErrorType = ErrorType.ConcurrencyConflict,
                            Message = "Concurrency_Conflict: The stock was just updated by another user."
                        };
                    }

                    _logger.LogInformation("Concurrency conflict detected. Reloading entities and retrying... (Attempt {Retry})", retry);

                    // Refresh the entity to get the latest values from the database
                    await _unitOfWork.Entry(variant).ReloadAsync();
                    await _unitOfWork.Entry(cart).ReloadAsync();
                }

            }

            return new ApiResponse<string>()
            {
                IsSuccess = false,
                ErrorType = ErrorType.ServerError,
                Message = "Operation failed."
            };
        }
        #endregion

        #region RemoveGuestItem
        public async Task<ApiResponse<string>> RemoveGuestItem(string guestId, int variantId)
        {
            _logger.LogInformation("Removing cart item for guest {GuestId}, variant {VariantId}", guestId, variantId);

            // 1. Fetch the data outside the retry loop
            GuestCart? cart = await _guestCartService.GetAsync(c => c.GuestId == guestId && c.VariantId == variantId);

            if (cart is null)
            {
                _logger.LogWarning("Cart item not found for guest {GuestId}, variant {VariantId}", guestId, variantId);
                return new ApiResponse<string>()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.NotFound,
                    Message = "Cart item not found!"
                };
            }

            Variant? variant = await _variantService.GetByIdAsync(variantId);

            int maxRetryAttempts = 3;

            for (int retry = 1; retry <= maxRetryAttempts; retry++)
            {
                try
                {
                    // 2. Perform calculations and deletion inside the loop so they apply to fresh data upon retry
                    if (variant is not null)
                    {
                        variant.Reserved -= cart.Quantity;
                        _logger.LogDebug("Decreased variant {VariantId} reserved count by {Quantity}", variantId, cart.Quantity);
                    }

                    _guestCartService.Delete(cart);

                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation("Successfully removed cart item for guest {GuestId}, variant {VariantId}", guestId, variantId);
                    return new ApiResponse<string>()
                    {
                        IsSuccess = true,
                        Message = "Item removed from cart successfully"
                    };
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogWarning(ex, "Concurrency conflict while removing item for guest {GuestId}, variant {VariantId}. Attempt {Attempt}/{MaxAttempts}", guestId, variantId, retry, maxRetryAttempts);

                    if (retry == maxRetryAttempts)
                    {
                        return new ApiResponse<string>()
                        {
                            IsSuccess = false,
                            ErrorType = ErrorType.ConcurrencyConflict,
                            Message = "Concurrency_Conflict: The system is busy, please try again."
                        };
                    }

                    _logger.LogInformation("Concurrency conflict detected. Reloading entities and retrying... (Attempt {Retry})", retry);

                    // 3. Reload the Variant entity if it exists
                    if (variant is not null)
                    {
                        await _unitOfWork.Entry(variant).ReloadAsync();
                    }

                    // 4. Pro trick: Reload the Cart inside its own try-catch block
                    try
                    {
                        await _unitOfWork.Entry(cart).ReloadAsync();
                    }
                    catch (InvalidOperationException)
                    {
                        // The magic happens here: If this error is thrown, it means a double-click occurred 
                        // and the concurrent request already deleted the cart!
                        // We don't need to continue the loop or decrease the Variant quantity again; 
                        // we just return a success response immediately.
                        _logger.LogInformation("Cart item was already removed by a concurrent request. Returning success implicitly for guest {GuestId}.", guestId);

                        return new ApiResponse<string>()
                        {
                            IsSuccess = true,
                            Message = "Item removed from cart successfully"
                        };
                    }
                }
            }

            return new ApiResponse<string>()
            {
                IsSuccess = false,
                ErrorType = ErrorType.ServerError,
                Message = "Operation failed."
            };
        }
        #endregion

        // merge method
        #region Merge
        public async Task<ApiResponse<bool>> Merge(int userId, string guestId, bool ignoreTransaction = false)
        {
            _logger.LogInformation("Merging guest cart for guest {GuestId} to user {userId}", guestId, userId);

            try
            {
                if (!ignoreTransaction) await _unitOfWork.BeginTransactionAsync();

                var guestCartItems = await _guestCartService.GetAllAsync(
                    filter: c => c.GuestId == guestId,
                    tracked: true,
                    includes: c => c.Variant);

                if (!guestCartItems.Any())
                {
                    _logger.LogInformation("Guest cart is empty for guest {GuestId}", guestId);
                    if (!ignoreTransaction) await _unitOfWork.CommitAsync();

                    return new()
                    {
                        IsSuccess = true,
                        Message = "Guest cart is empty",
                    };
                }

                var userCartItems = await _userCartService.GetAllAsync(
                    filter: c => c.UserId == userId,
                    tracked: true,
                    includes: c => c.Variant);

                // Add the items from the guest to user cart
                foreach (var guestCart in guestCartItems)
                {
                    _logger.LogDebug("Processing guest cart item - VariantId: {VariantId}, Quantity: {Quantity}", guestCart.VariantId, guestCart.Quantity);

                    var userCartItem = userCartItems.FirstOrDefault(uc => uc.VariantId == guestCart.VariantId);

                    // if the auth user has the current item in his cart, increase the amount, otherwise add it.
                    if (userCartItem is not null)
                    {
                        userCartItem.Quantity += guestCart.Quantity;
                        _logger.LogDebug("Updated existing user cart item for variant {VariantId} to quantity {Quantity}", guestCart.VariantId, userCartItem.Quantity);
                    }
                    else
                    {
                        UserCart newUserCart = new()
                        {
                            UserId = userId,
                            VariantId = guestCart.VariantId,
                            Quantity = guestCart.Quantity,
                        };

                        _userCartService.Add(newUserCart);
                        _logger.LogDebug("Created new user cart item for variant {VariantId}, quantity {Quantity}", guestCart.VariantId, guestCart.Quantity);
                    }
                }

                _logger.LogInformation("Batch deleting {Count} items from guest cart {GuestId}", guestCartItems.Count(), guestId);
                _guestCartService.DeleteRange(guestCartItems);

                await _unitOfWork.SaveChangesAsync();
                if (!ignoreTransaction) await _unitOfWork.CommitAsync();

                _logger.LogInformation("Successfully merged guest cart to user cart for guest {GuestId} with {ItemCount} items", guestId, guestCartItems.Count());

                return new()
                {
                    IsSuccess = true,
                    Message = "Cart merged successfully",
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error merging guest cart for guest {GuestId}", guestId);
                if (!ignoreTransaction) await _unitOfWork.RollbackAsync();

                return new()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.ServerError,
                    Message = "Something went wrong while merging cart"
                };
            }
        }
        #endregion
    }
}
