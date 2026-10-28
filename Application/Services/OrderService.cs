using Application.DTOs;
using Application.Interfaces;
using Application.Mapping;
using Application.Responses;
using Core.Entities;
using Core.Enum;
using Core.Enums;
using Core.Interfaces;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.Logging;
using System.Buffers.Text;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Application.Services
{
    public class OrderService : Services<Order>, IOrderService
    {
        private readonly ICartService _cartServices;
        private readonly IServices<UserCart> _userCartServices;
        private readonly IServices<OrderDetail> _orderDetailService;
        private readonly IServices<InventoryTransaction> _inventoryTransactionService;
        private readonly IServices<Language> _langServices;
        private readonly UserManager<User> _userManager;
        private readonly IAuthService _authService;

        public OrderService(
            IRepo<Order> repo,
            IUnitOfWork unitOfWork,
            ILogger<OrderService> logger,
            ICartService cartServices,
            IServices<UserCart> userCartServices,
            IServices<OrderDetail> orderDetailService,
            IServices<InventoryTransaction> inventoryTransactionService,
            IServices<Language> lang,
            UserManager<User> userManager,
            IAuthService authService) : base(unitOfWork, repo, logger)
        {
            _cartServices = cartServices;
            _userCartServices = userCartServices;
            _orderDetailService = orderDetailService;
            _inventoryTransactionService = inventoryTransactionService;
            _langServices = lang;
            _userManager = userManager;
            _authService = authService;
        }

        public async Task<IEnumerable<Order>> FetchUserOrdersWithDetailsAsync(int userId, int langId)
        {
            _logger.LogInformation("Fetching order entities with details from DB for user {UserId} with Language ID {LangId}", userId, langId);

            return await Query(o => o.UserId == userId, tracked: false)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.Translations.Where(t => t.LanguageId == langId))
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Variant)
                        .ThenInclude(v => v.Color)
                            .ThenInclude(c => c.Translations.Where(t => t.LanguageId == langId))
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Variant)
                        .ThenInclude(v => v.Size)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Variant)
                        .ThenInclude(v => v.Images)
                            .ThenInclude(i => i.MediaAsset)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }
        public async Task<ApiResponse<IEnumerable<OrderHistoryDto>>> UserOrderHistory(int userId, string langCode, string baseUrl)
        {
            _logger.LogInformation("Retrieve all orders for userId: {userId}", userId);
            _logger.LogInformation("Retrieving language ID for language code: {LangCode}", langCode);
            int langId = await _langServices.Query(l => l.Code == langCode).Select(l => l.Id).FirstOrDefaultAsync();

            if (langId == 0)
            {
                _logger.LogWarning("Language code {LangCode} not found. Defaulting to language ID 1.", langCode);
                langId = 1; // Default to Arabic if not found
            }
            _logger.LogInformation("Found language ID {LangId} for language code {LangCode}", langId, langCode);

            var orders = await FetchUserOrdersWithDetailsAsync(userId, langId);
            _logger.LogInformation("Orders with details retrieved successfully");

            var ordersHistoryDto = orders.ToHistoryDtoList(langId, baseUrl) ?? new();

            return new()
            {
                IsSuccess = true,
                Message = "Orders with details retrieved successfully",
                Data = ordersHistoryDto
            };
        }


        public async Task<ApiResponse<(AuthServiceResponse? authResponse, CheckoutResponseDto checkoutResponse)>> ProcessCheckoutAsync(CheckoutRequestDto request, string? userId, string baseUrl)
        {
            _logger.LogInformation("Checkout process started with IdempotencyKey: {IdempotencyKey}", request.IdempotencyKey);

            try
            {
                // STEP 1 - AUTHENTICATION HANDLING
                #region handle authentication
                _logger.LogInformation("Step 1: Authentication handling - IsAuthenticated: {IsAuthenticated}", request.IsAuthenticated);

                User? user = null;
                AuthServiceResponse? authResponse = null;

                // If the user is not authenticated, handle authentication (login or registration)
                if (!request.IsAuthenticated)
                {
                    _logger.LogInformation("User is not authenticated, proceeding with authentication handling");

                    var result = await HandleUserAuthenticationAsync(request);

                    if (!result.IsSuccess)
                    {
                        _logger.LogError("Authentication handling failed - Error: {ErrorMessage}", result.Message);

                        return new()
                        {
                            IsSuccess = false,
                            ErrorType = ErrorType.ValidationError,
                            Message = result.Message
                        };
                    }

                    authResponse = result.Data;
                    user = await _userManager.FindByEmailAsync(request.Email);
                    _logger.LogInformation("User authenticated successfully - UserId: {UserId}, Email: {Email}", user.Id, user.Email);
                }
                else
                {
                    user = !string.IsNullOrEmpty(userId) ? await _userManager.FindByIdAsync(userId) : null;

                    if (user is null)
                    {
                        _logger.LogInformation("User not found or user id is null: {userId}", userId);

                        return new()
                        {
                            IsSuccess = false,
                            ErrorType = ErrorType.NotFound,
                            Message = "User not found"
                        };
                    }
                    _logger.LogInformation("user found successfully with Id: {userId}, and email: {email}", user?.Id, user?.Email);
                }

                #endregion

                // STEP 2 - START DATABASE TRANSACTION
                #region DATABASE TRANSACTION
                _logger.LogInformation("Step 2: Beginning database transaction");
                await _unitOfWork.BeginTransactionAsync();
                #endregion

                // STEP 3 - IDEMPOTENCY CHECK
                #region idempotency check
                //_logger.LogInformation("Step 2: Checking for existing order with IdempotencyKey: {IdempotencyKey}", request.IdempotencyKey);
                //var existingOrder = await GetAsync(
                //    filter: o => o.Number == request.IdempotencyKey,
                //    tracked: false);

                //if (existingOrder != null)
                //{
                //    _logger.LogWarning("Idempotent request: Order already exists with IdempotencyKey: {IdempotencyKey}, OrderId: {OrderId}", 
                //        request.IdempotencyKey, existingOrder.Id);

                //    await _unitOfWork.CommitAsync();

                //    return new ApiResponse<CheckoutResponseDto>()
                //    {
                //        isSucess = true,
                //        Message = "Order already exists (Idempotent request)",
                //        Data = new CheckoutResponseDto
                //        {
                //            OrderId = existingOrder.Id,
                //            OrderNumber = existingOrder.Number,
                //            CreatedAt = existingOrder.CreatedAt,
                //            TotalPrice = existingOrder.TotalPrice
                //        }
                //    };
                //}
                #endregion

                // STEP 4 - CART HANDLING
                #region CART HANDLING
                _logger.LogInformation("Step 4: Handling cart for UserId: {UserId} with GuestId: {GuestId}", user.Id, request.GuestId);
                var userCart = await GerCart(request.GuestId, user.Id);

                if (!userCart.IsSuccess)
                {
                    _logger.LogError("Cart handling failed for UserId: {UserId} - Error: {ErrorMessage}", user.Id, userCart.Message);
                    await _unitOfWork.RollbackAsync();

                    return new()
                    {
                        IsSuccess = false,
                        ErrorType = userCart.ErrorType,
                        Message = $"Cart handling failed: {userCart.Message}"
                    };
                }
                #endregion

                // STEP 5 - CREATE ORDER
                #region CREATE ORDER
                _logger.LogInformation("Step 5: Creating order for UserId: {UserId}", user.Id);
                var orderResponse = await CreateOrder(user.Id, userCart.Data, request);

                if (!orderResponse.IsSuccess)
                {
                    _logger.LogError("Order creation failed for UserId: {UserId} - Error: {ErrorMessage}", user.Id, orderResponse.Message);
                    await _unitOfWork.RollbackAsync();

                    return new()
                    {
                        IsSuccess = false,
                        ErrorType = orderResponse.ErrorType,
                        Message = $"Order creation failed: {orderResponse.Message}"
                    };
                }

                _logger.LogInformation("Order created successfully with Id: {orderId}, for user: {userId}", orderResponse.Data.Id, orderResponse.Data.UserId);
                Order order = orderResponse.Data;
                #endregion

                //STEP 6 - CREATE ORDER DETAILS
                #region CREATE ORDER DETAILS
                _logger.LogInformation("Step 6: Creating order details for OrderId: {OrderId}", order.Id);

                var orderDetailsReponse = await CreateOrderDetails(order.Id, userCart.Data);

                if (!orderDetailsReponse.IsSuccess)
                {
                    _logger.LogError("Order details creation failed for OrderId: {OrderId} - Error: {ErrorMessage}", order.Id, orderDetailsReponse.Message);
                    await _unitOfWork.RollbackAsync();
                    return new()
                    {
                        IsSuccess = false,
                        ErrorType = orderDetailsReponse.ErrorType,
                        Message = $"Order details creation failed: {orderDetailsReponse.Message}"
                    };
                }
                #endregion

                // STEP 7 - VARIANT RESERVATION
                #region VARIANT RESERVATION
                _logger.LogInformation("Step 7: Processing variant reservations");

                var stockUpdateResponse = await UpdateStockQuantities(user.Id, userCart.Data);

                if (!stockUpdateResponse.IsSuccess)
                {
                    _logger.LogError("Variant reservation failed for UserId: {UserId} - Error: {ErrorMessage}", user.Id, stockUpdateResponse.Message);
                    await _unitOfWork.RollbackAsync();

                    return new()
                    {
                        IsSuccess = false,
                        ErrorType = stockUpdateResponse.ErrorType,
                        Message = $"Variant reservation failed: {stockUpdateResponse.Message}"
                    };
                }
                #endregion

                // STEP 8 - EMPTYING THE CART
                #region EMPTYING THE CART
                _logger.LogInformation("Step 8: Emptying the used cart");

                var emptyCartResponse = await EmptyCart(user.Id, userCart.Data);

                if (!emptyCartResponse.IsSuccess)
                {
                    _logger.LogError("Emptying cart failed for UserId: {UserId} - Error: {ErrorMessage}", user.Id, emptyCartResponse.Message);
                    await _unitOfWork.RollbackAsync();

                    return new()
                    {
                        IsSuccess = false,
                        ErrorType = emptyCartResponse.ErrorType,
                        Message = $"Emptying cart failed: {emptyCartResponse.Message}"
                    };
                }
                #endregion

                // STEP 9 - SAVE INVENTORY TRANSACTION RECORD
                #region INVENTORY TRANSACTION
                _logger.LogInformation("Step 9: Create inventory transactions");
                var inventoryTransactionResponse = await CreateInventoryTransactions(userCart.Data, order.Id);

                if (!inventoryTransactionResponse.IsSuccess)
                {
                    _logger.LogError("Creating inventory transactions failed for OrderId: {OrderId} - Error: {ErrorMessage}", order.Id, inventoryTransactionResponse.Message);
                    await _unitOfWork.RollbackAsync();
                    return new()
                    {
                        IsSuccess = false,
                        ErrorType = inventoryTransactionResponse.ErrorType,
                        Message = $"Creating inventory transactions failed: {inventoryTransactionResponse.Message}"
                    };
                }
                #endregion

                // STEP 10 - COMMIT TRANSACTION
                #region COMMIT TRANSACTION & CREATE CHECKOUT RESPONSE
                _logger.LogInformation("Step 10: Committing transaction");
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Checkout completed successfully - OrderId: {OrderId}, OrderNumber: {OrderNumber}",
                    order.Id, order.Number);

                var checkoutResponse = new CheckoutResponseDto
                {
                    OrderId = order.Id,
                    OrderNumber = order.Number,
                    CreatedAt = order.CreatedAt,
                    TotalPrice = order.TotalPrice
                };
                #endregion

                // STEP 11 - SEND ADMIN EMAIL
                #region SEND ADMIN EMAIL
                BackgroundJob.Enqueue<IEmailService>(e => e.SendAdminEmailAsync(new EmailInfoDto()
                {
                    OrderNumber = order.Number,
                    FName = request.FirstName,
                    LName = request.LastName,
                    Email = request.Email,
                    Phone = request.Phone,
                    Address = request.Address,
                    City = request.City,
                    Governorate = request.Governorate,
                    Items = userCart.Data.ToDtoList(baseUrl),
                    TotalPrice = order.TotalPrice
                }));
                #endregion

                return new()
                {
                    IsSuccess = true,
                    Message = "Order created successfully",
                    Data = (authResponse, checkoutResponse)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Checkout process failed");

                try
                {
                    await _unitOfWork.RollbackAsync();
                    _logger.LogInformation("Transaction rolled back successfully");
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "Error during transaction rollback");
                }

                return new()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.ServerError,
                    Message = $"Checkout failed: {ex.Message}"
                };
            }
        }

        // checkout methods
        private async Task<ApiResponse<AuthServiceResponse>> HandleUserAuthenticationAsync(CheckoutRequestDto request)
        {
            User? user = null;

            // Check if user exists with the provided email
            _logger.LogInformation("User is not authenticated, check if the user has an acount: {Email}", request.Email);
            user = await _userManager.FindByEmailAsync(request.Email);

            if (user is not null)
            {
                _logger.LogInformation("User found with email: {Email}, attempting to log in", request.Email);

                var loginDto = new LoginDto
                {
                    Email = request.Email,
                    Password = request.Password
                };

                var loginResponse = await _authService.Login(loginDto);

                if (loginResponse.IsSuccess)
                {
                    _logger.LogInformation("Login successfully with email: {Email}", request.Email);

                    return new()
                    {
                        IsSuccess = true,
                        Message = loginResponse.Message,
                        Data = loginResponse
                    };
                }
            }

            _logger.LogWarning("No existing user found with email: {Email}, proceeding to register a new user", request.Email);
            var registerDto = new RegisterDto
            {
                Fname = request.FirstName,
                Lname = request.LastName,
                Email = request.Email,
                PhoneNumber = request.Phone,
                Password = request.Password,
                ConfirmPassword = request.Password
            };

            var registerResponse = await _authService.Register(registerDto);

            if (!registerResponse.IsSuccess)
            {
                _logger.LogError("User registration failed for email: {Email} - Error: {ErrorMessage}", request.Email, registerResponse.Message);

                return new()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.ValidationError,
                    Message = registerResponse.Message,
                };
            }
            _logger.LogInformation("User registration successful for email: {Email}, retrieving the user after registration", request.Email);

            user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
            {
                _logger.LogInformation("user not found after registration");

                return new()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.ServerError,
                    Message = "user not found after registration"
                };
            }

            _logger.LogInformation("User retrieved successfully after registration");
            return new()
            {
                IsSuccess = true,
                Message = "User registered successfully",
                Data = registerResponse
            };
        }

        private async Task<ApiResponse<IEnumerable<UserCart>>> GerCart(string? guestId, int userId)
        {
            if (!string.IsNullOrEmpty(guestId))
            {
                // Merge guest cart into user cart
                _logger.LogInformation("Merget guest cart for guestId: {guestId} into user cart for userId: {userId}", guestId, userId);
                var result = await _cartServices.Merge(userId, guestId, true);

                if (!result.IsSuccess)
                {
                    _logger.LogError("Failed to merge guest cart into user cart - GuestId: {GuestId}, UserId: {UserId} - Error: {ErrorMessage}",
                        guestId, userId, result.Message);

                    return new()
                    {
                        IsSuccess = false,
                        ErrorType = ErrorType.ServerError,
                        Message = $"Failed to merge guest cart: {result.Message}"
                    };
                }
            }

            // Retrieve user cart items
            var userCart = await _cartServices.FetchCartWithDetailsAsync(userId, 1);

            //if (!userCart.IsSuccess)
            //{
            //    _logger.LogWarning("Failed to retrieve user cart for userId: {userId}", userId);
            //    return new()
            //    {
            //        IsSuccess = false,
            //        Message = $"Failed to retrieve user cart for userId: {userCart.Message}"
            //    };
            //}

            if (!userCart.Any())
            {
                _logger.LogInformation("Empty cart for userId: {userId}", userId);

                return new()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.ValidationError,
                    Message = "User does not have any item in his cart!"
                };
            }

            return new()
            {
                IsSuccess = true,
                Message = "User cart retrieved successfully",
                Data = userCart
            };
        }

        private async Task<ApiResponse<Order>> CreateOrder(int userId, IEnumerable<UserCart> userCarts, CheckoutRequestDto request)
        {
            decimal totalPrice = userCarts.Sum(c => c.Variant.Product.Price * c.Quantity);
            _logger.LogInformation("The total price for the order is calculated: {TotalPrice}", totalPrice);

            var order = new Order
            {
                UserId = userId,
                Number = await GenerateOrderNumberAsync(),
                TotalPrice = totalPrice,
                FullAddress = request.Address,
                Governorate = request.Governorate,
                City = request.City,
                Status = OrderStatus.Confirmed,
            };

            Add(order);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Order created with OrderId: {OrderId}, OrderNumber: {OrderNumber}, created at: {createdAt}", order.Id, order.Number, order.CreatedAt);

            return new()
            {
                IsSuccess = true,
                Message = "Order created successfully",
                Data = order
            };
        }

        private async Task<ApiResponse<bool>> CreateOrderDetails(int orderId, IEnumerable<UserCart> usercar)
        {
            foreach (var item in usercar)
            {
                _logger.LogInformation("Creating order detail for OrderId: {OrderId}, VariantId: {VariantId}, Quantity: {Quantity}",
                    orderId, item.VariantId, item.Quantity);

                OrderDetail orderDetail = new()
                {
                    OrderId = orderId,
                    VariantId = item.VariantId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Variant.Product.Price,
                    TotalPrice = item.Variant.Product.Price * item.Quantity
                };

                _orderDetailService.Add(orderDetail);
                _logger.LogInformation("Order detail created for OrderId: {OrderId}, VariantId: {VariantId}, Quantity: {Quantity}",
                    orderId, item.VariantId, item.Quantity);
            }
            await _unitOfWork.SaveChangesAsync();

            return new()
            {
                IsSuccess = true,
                Message = "Order details created successfully"
            };
        }

        private async Task<ApiResponse<string>> UpdateStockQuantities(int userId, IEnumerable<UserCart> userCarts)
        {
            foreach (var cartItem in userCarts)
            {
                var variant = cartItem.Variant;

                _logger.LogInformation("Processing variant - VariantId: {VariantId}, Requested Qty: {RequestedQty}, Available: {AvailableQty}",
                    variant.Id, cartItem.Quantity, variant.Quantity);

                if (cartItem.Quantity > variant.Quantity)
                {
                    _logger.LogWarning("Insufficient stock for VariantId: {VariantId} - Requested: {RequestedQty}, Available: {AvailableQty}",
                        variant.Id, cartItem.Quantity, variant.Quantity);

                    return new()
                    {
                        IsSuccess = false,
                        ErrorType = ErrorType.InsufficientStock,
                        Message = $"Insufficient stock for variant {variant.Id} - Requested: {cartItem.Quantity}, Available: {variant.Quantity}"
                    };
                }

                // Reserve stock by reducing available quantity and increasing reserved quantity
                variant.Quantity -= cartItem.Quantity;
                variant.Reserved -= cartItem.Quantity;

                _logger.LogInformation("Variant stock updated - VariantId: {VariantId}, New Available: {NewAvailable}, New Reserved: {NewReserved}",
                    variant.Id, variant.Quantity, variant.Reserved);
            }

            return new()
            {
                IsSuccess = true,
                Message = "Stock quantities updated and cart item deleted successfully"
            };
        }

        private async Task<ApiResponse<bool>> EmptyCart(int userId, IEnumerable<UserCart> userCart)
        {
            _logger.LogInformation("Emptying cart for UserId: {UserId}", userId);
            foreach (var cartItem in userCart)
            {
                _logger.LogInformation("Deleting cart item - UserId: {UserId}, VariantId: {VariantId}", userId, cartItem.VariantId);

                _userCartServices.Delete(cartItem);

                _logger.LogInformation("Cart item deleted successfully - UserId: {UserId}, VariantId: {VariantId}", userId, cartItem.VariantId);
            }
            return new()
            {
                IsSuccess = true,
                Message = "Cart emptied successfully"
            };
        }

        private async Task<ApiResponse<bool>> CreateInventoryTransactions(IEnumerable<UserCart> cart, int orderId)
        {
            foreach (var item in cart)
            {
                InventoryTransaction transaction = new()
                {
                    OrderId = orderId,
                    UserId = item.UserId,
                    VariantId = item.VariantId,
                    Quantity = item.Quantity,
                    TransactionType = InventoryTransactionType.Sale,
                };

                _inventoryTransactionService.Add(transaction);
                _logger.LogInformation("Inventory transaction created successfully for OrderId: {OrderId}, VariantId: {VariantId}", orderId, item.VariantId);
            }

            await _unitOfWork.SaveChangesAsync();
            return new()
            {
                IsSuccess = true,
                Message = "Inventory transactions created successfully"
            };
        }

        public async Task<string> GenerateOrderNumberAsync()
        {
            _logger.LogInformation("Generating unique order number");

            Random random = new Random();
            int randomNumber = random.Next(100000, 999999);

            string orderNumber = $"ORD-{DateTimeOffset.Now.ToUnixTimeMilliseconds() / randomNumber}";

            while (await ExistsAsync(o => o.Number == orderNumber))
            {
                _logger.LogWarning("Generated order number already exists: {OrderNumber}, generating a new one", orderNumber);
                orderNumber = $"ORD-{DateTimeOffset.Now.ToUnixTimeMilliseconds()}";
            }

            return orderNumber;
        }
    }
}
