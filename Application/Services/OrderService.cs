using Application.DTOs;
using Application.Interfaces;
using Application.Responses;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Application.Services
{
    public class OrderService : Services<Order>, IOrderService
    {
        private readonly IOrderRepo _orderRepo;
        private readonly ICartService _cartServices;
        private readonly IServices<UserCart> _userCartServices;
        private readonly IServices<OrderDetail> _orderDetailService;
        private readonly IServices<InventoryTransaction> _inventoryTransactionService;
        private readonly UserManager<User> _userManager;
        private readonly IAuthService _authService;
        private readonly IMapper _mapper;

        public OrderService(
            IRepo<Order> repo,
            IUnitOfWork unitOfWork,
            ILogger<OrderService> logger,
            IOrderRepo orderRepo,
            ICartService cartServices,
            IServices<UserCart> userCartServices,
            IServices<OrderDetail> orderDetailService,
            IServices<InventoryTransaction> inventoryTransactionService,
            UserManager<User> userManager,
            IAuthService authService,
            IMapper mapper) : base(unitOfWork, repo, logger)
        {
            _orderRepo = orderRepo;
            _cartServices = cartServices;
            _userCartServices = userCartServices;
            _orderDetailService = orderDetailService;
            _inventoryTransactionService = inventoryTransactionService;
            _userManager = userManager;
            _authService = authService;
            _mapper = mapper;
        }

        /// <summary>
        /// Processes checkout with complete order creation workflow including:
        /// - Transaction management
        /// - Idempotency check
        /// - User authentication handling
        /// - Guest cart merge
        /// - Order and OrderDetails creation
        /// - Variant reservation with stock validation
        /// - Inventory transaction recording
        /// </summary>
        /// <param name="request">Checkout request with cart, userId details</param>
        /// <returns>API response with order informations</returns>
        public async Task<ApiResponse<(AuthServiceResponse? authResponse, CheckoutResponseDto checkoutResponse)>> ProcessCheckoutAsync(CheckoutRequestDto request, string? userId)
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

                    if (!result.IsSucess)
                    {
                        _logger.LogError("Authentication handling failed - Error: {ErrorMessage}", result.Message);

                        return new()
                        {
                            IsSucess = false,
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
                            IsSucess = false,
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

                if (!userCart.IsSucess)
                {
                    _logger.LogError("Cart handling failed for UserId: {UserId} - Error: {ErrorMessage}", user.Id, userCart.Message);
                    await _unitOfWork.RollbackAsync();

                    return new()
                    {
                        IsSucess = false,
                        Message = $"Cart handling failed: {userCart.Message}"
                    };
                }
                #endregion

                // STEP 5 - CREATE ORDER
                #region CREATE ORDER
                _logger.LogInformation("Step 5: Creating order for UserId: {UserId}", user.Id);
                var orderResponse = await CreateOrder(user.Id, userCart.Data, request);

                if (!orderResponse.IsSucess)
                {
                    _logger.LogError("Order creation failed for UserId: {UserId} - Error: {ErrorMessage}", user.Id, orderResponse.Message);
                    await _unitOfWork.RollbackAsync();

                    return new()
                    {
                        IsSucess = false,
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

                if (!orderDetailsReponse.IsSucess)
                {
                    _logger.LogError("Order details creation failed for OrderId: {OrderId} - Error: {ErrorMessage}", order.Id, orderDetailsReponse.Message);
                    await _unitOfWork.RollbackAsync();
                    return new()
                    {
                        IsSucess = false,
                        Message = $"Order details creation failed: {orderDetailsReponse.Message}"
                    };
                }
                #endregion

                // STEP 7 - VARIANT RESERVATION
                #region VARIANT RESERVATION
                _logger.LogInformation("Step 7: Processing variant reservations");

                var stockUpdateResponse = await UpdateStockQuantities(user.Id, userCart.Data);

                if (!stockUpdateResponse.IsSucess)
                {
                    _logger.LogError("Variant reservation failed for UserId: {UserId} - Error: {ErrorMessage}", user.Id, stockUpdateResponse.Message);
                    await _unitOfWork.RollbackAsync();

                    return new()
                    {
                        IsSucess = false,
                        Message = $"Variant reservation failed: {stockUpdateResponse.Message}"
                    };
                }
                #endregion

                // STEP 8 - EMPTYING THE CART
                #region EMPTYING THE CART
                _logger.LogInformation("Step 8: Emptying the used cart");

                var emptyCartResponse = await EmptyCart(user.Id, userCart.Data);

                if (!emptyCartResponse.IsSucess)
                {
                    _logger.LogError("Emptying cart failed for UserId: {UserId} - Error: {ErrorMessage}", user.Id, emptyCartResponse.Message);
                    await _unitOfWork.RollbackAsync();

                    return new()
                    {
                        IsSucess = false,
                        Message = $"Emptying cart failed: {emptyCartResponse.Message}"
                    };
                }
                #endregion

                // STEP 9 - SAVE INVENTORY TRANSACTION RECORD
                #region INVENTORY TRANSACTION
                _logger.LogInformation("Step 9: Create inventory transactions");
                var inventoryTransactionResponse = await CreateInventoryTransactions(userCart.Data, order.Id);

                if (!inventoryTransactionResponse.IsSucess)
                {
                    _logger.LogError("Creating inventory transactions failed for OrderId: {OrderId} - Error: {ErrorMessage}", order.Id, inventoryTransactionResponse.Message);
                    await _unitOfWork.RollbackAsync();
                    return new()
                    {
                        IsSucess = false,
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
                    Items = _mapper.MapToDtoList(userCart.Data),
                    TotalPrice = order.TotalPrice
                }));
                #endregion

                return new()
                {
                    IsSucess = true,
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
                    IsSucess = false,
                    Message = $"Checkout failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Retrieves the full order history for a specific user.
        /// Includes nested details such as product variants, translations, and associated media.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose order history is being requested.</param>
        /// <returns>
        /// An asynchronous task that returns an <see cref="ApiResponse{T}"/> containing a list of <see cref="Order"/> objects.
        /// </returns>
        public async Task<ApiResponse<List<Order>>> OrderHistory(int userId)
        {
            _logger.LogInformation("Retrieve all orders for userId: {userId}", userId);

            var orders = Query(o => o.UserId == userId)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.Translations)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Variant)
                        .ThenInclude(v => v.Color)
                            .ThenInclude(c => c.Translations)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Variant)
                        .ThenInclude(v => v.Size)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Variant)
                        .ThenInclude(v => v.Images)
                            .ThenInclude(i => i.MediaAsset)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            _logger.LogInformation("Orders retrieved successfully");

            return new()
            {
                IsSucess = true,
                Data = orders
            };
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
                        IsSucess = true,
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
                    IsSucess = false,
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
                    IsSucess = false,
                    Message = "user not found after registration"
                };
            }

            _logger.LogInformation("User retrieved successfully after registration");
            return new()
            {
                IsSucess = true,
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

                if (!result.IsSucess)
                {
                    _logger.LogError("Failed to merge guest cart into user cart - GuestId: {GuestId}, UserId: {UserId} - Error: {ErrorMessage}",
                        guestId, userId, result.Message);

                    return new()
                    {
                        IsSucess = false,
                        Message = $"Failed to merge guest cart: {result.Message}"
                    };
                }
            }

            // Retrieve user cart items
            var userCart = await _cartServices.GetUserCart(userId, 1);

            if (!userCart.IsSucess)
            {
                _logger.LogWarning("Failed to retrieve user cart for userId: {userId}", userId);
                return new()
                {
                    IsSucess = false,
                    Message = $"Failed to retrieve user cart for userId: {userCart.Message}"
                };
            }

            if (!userCart.Data.Any())
            {
                _logger.LogInformation("Empty cart for userId: {userId}", userId);

                return new()
                {
                    IsSucess = false,
                    Message = "User does not have any item in his cart!"
                };
            }

            return new()
            {
                IsSucess = true,
                Message = "User cart retrieved successfully",
                Data = userCart.Data
            };
        }

        private async Task<ApiResponse<Order>> CreateOrder(int userId, IEnumerable<UserCart> userCarts, CheckoutRequestDto request)
        {
            try
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
                    CreatedAt = DateTime.Now
                };
                await AddAsync(order);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Order created with OrderId: {OrderId}, OrderNumber: {OrderNumber}, created at: {createdAt}", order.Id, order.Number, order.CreatedAt);

                return new()
                {
                    IsSucess = true,
                    Message = "Order created successfully",
                    Data = order
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order for UserId: {UserId}", userId);

                return new()
                {
                    IsSucess = false,
                    Message = $"Error creating order: {ex.Message}"
                };
            }
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

                try
                {
                    await _orderDetailService.AddAsync(orderDetail);
                    _logger.LogInformation("Order detail created for OrderId: {OrderId}, VariantId: {VariantId}, Quantity: {Quantity}",
                        orderId, item.VariantId, item.Quantity);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating order detail for OrderId: {OrderId}, VariantId: {VariantId}", orderId, item.VariantId);
                    return new()
                    {
                        IsSucess = false,
                        Message = $"Error creating order detail for variant {item.VariantId}: {ex.Message}"
                    };
                }
            }
            await _unitOfWork.SaveChangesAsync();

            return new()
            {
                IsSucess = true,
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
                        IsSucess = false,
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
                IsSucess = true,
                Message = "Stock quantities updated and cart item deleted successfully"
            };
        }

        private async Task<ApiResponse<bool>> EmptyCart(int userId, IEnumerable<UserCart> userCart)
        {
            _logger.LogInformation("Emptying cart for UserId: {UserId}", userId);
            foreach (var cartItem in userCart)
            {
                _logger.LogInformation("Deleting cart item - UserId: {UserId}, VariantId: {VariantId}", userId, cartItem.VariantId);

                try
                {
                    await _userCartServices.DeleteAsync(cartItem);
                }
                catch (Exception ex)
                {
                    return new()
                    {
                        IsSucess = false,
                        Message = $"Failed to delete cart item for variant {cartItem.VariantId}: {ex.Message}"
                    };
                }

                _logger.LogInformation("Cart item deleted successfully - UserId: {UserId}, VariantId: {VariantId}", userId, cartItem.VariantId);
            }
            return new()
            {
                IsSucess = true,
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
                    CreatedAt = DateTime.Now
                };

                try
                {
                    await _inventoryTransactionService.AddAsync(transaction);
                    _logger.LogInformation("Inventory transaction created successfully for OrderId: {OrderId}, VariantId: {VariantId}", orderId, item.VariantId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating inventory transaction for OrderId: {OrderId}, VariantId: {VariantId}", orderId, item.VariantId);
                    return new()
                    {
                        IsSucess = false,
                        Message = $"Error creating inventory transaction for variant {item.VariantId}: {ex.Message}"
                    };
                }
            }

            await _unitOfWork.SaveChangesAsync();
            return new()
            {
                IsSucess = true,
                Message = "Inventory transactions created successfully"
            };
        }

        public async Task<string> GenerateOrderNumberAsync()
        {
            _logger.LogInformation("Generating unique order number");

            string orderNumber = $"ORD-{DateTimeOffset.Now.ToUnixTimeMilliseconds()}";

            while (await ExistsAsync(o => o.Number == orderNumber))
            {
                _logger.LogWarning("Generated order number already exists: {OrderNumber}, generating a new one", orderNumber);
                orderNumber = $"ORD-{DateTimeOffset.Now.ToUnixTimeMilliseconds()}";
            }

            return orderNumber;
        }
    }
}
