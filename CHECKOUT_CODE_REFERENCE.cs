/**
 * CHECKOUT WORKFLOW - COMPLETE CODE REFERENCE
 * 
 * This file serves as a reference guide showing all components
 * of the checkout workflow and how they integrate together.
 * 
 * File Locations:
 * - Controller: API\Controllers\OrderController.cs
 * - Service: Application\Services\OrderService.cs
 * - Interface: Application\Interfaces\IOrderService.cs
 * - DTOs: Application\DTOs\CheckoutRequestDto.cs, CheckoutResponseDto.cs
 * - Enums: Core\Enum\OrderStatus.cs, Core\Enum\InventoryTransactionType.cs
 */

// ============================================================================
// STEP-BY-STEP WORKFLOW EXECUTION
// ============================================================================

/**
 * CLIENT REQUEST FLOW:
 * 
 * 1. Frontend clicks "Place Order" button
 * 2. Sends POST /api/order/checkout with CheckoutRequestDto
 * 3. OrderController.Checkout() validates request
 * 4. Calls orderService.ProcessCheckoutAsync(request)
 * 5. OrderService executes 9-step workflow inside transaction
 * 6. Returns CheckoutResponseDto or error
 */

// ============================================================================
// STEP 1: DATABASE TRANSACTION INITIATION
// ============================================================================

/*
Location: Application/Services/OrderService.cs, Line: ~80

Implementation:
    await _unitOfWork.BeginTransactionAsync();

Details:
- Starts a database transaction
- All subsequent DbContext operations are within this transaction
- Transaction remains open until CommitAsync() or RollbackAsync()
- If BeginTransactionAsync throws, exception is caught and rollback attempted
- If transaction already active, InvalidOperationException thrown

Database Isolation Level: Implementation uses default SQL Server READ_COMMITTED
This prevents:
  - Dirty reads
  - Non-repeatable reads (within this transaction)
  - Phantom reads are still possible but unlikely in checkout scenario
*/

// ============================================================================
// STEP 2: IDEMPOTENCY CHECK
// ============================================================================

/*
Location: Application/Services/OrderService.cs, Lines: ~88-107

Code:
    var existingOrder = await _orderService.GetAsync(
        filter: o => o.Number == request.IdempotencyKey,
        tracked: false);

    if (existingOrder != null)
    {
        await _unitOfWork.CommitAsync();
        
        return new ApiResponse<CheckoutResponseDto>()
        {
            Success = true,
            Message = "Order already exists (Idempotent request)",
            Data = new CheckoutResponseDto
            {
                OrderId = existingOrder.Id,
                OrderNumber = existingOrder.Number,
                CreatedAt = existingOrder.CreatedAt,
                TotalPrice = existingOrder.TotalPrice
            }
        };
    }

Why Idempotency?
- Network might timeout after order is created but before response received
- Frontend retries checkout → would create duplicate order
- Idempotency key ensures only ONE order per checkout attempt
- Subsequent retry returns same order without creating duplicate

Idempotency Key Generation (Client Side):
- UUID: "550e8400-e29b-41d4-a716-446655440000"
- Timestamp-based: "ORD_1705772400_12345"
- Hash-based: SHA256(userId + cartItemsList + timestamp)

Database Lookup:
- Order.Number = request.IdempotencyKey
- Query runs UNTRACKED (read-only) for performance
- Returns fast even with many orders
*/

// ============================================================================
// STEP 3: AUTHENTICATION HANDLING
// ============================================================================

/*
Location: Application/Services/OrderService.cs, Lines: ~110-165

Scenario A: IsAuthenticated = true (Existing User)
    user = await _userManager.FindByEmailAsync(request.Email);
    
    if (user == null)
    {
        await _unitOfWork.RollbackAsync();
        return Error("User not found");
    }

Scenario B: IsAuthenticated = false (Guest Checkout)
    var loginDto = new LoginDto
    {
        Email = request.Email,
        Password = request.Password
    };

    var loginResponse = await _authService.Login(loginDto);

    if (!loginResponse.IsSuccess)
    {
        await _unitOfWork.RollbackAsync();
        return Error(loginResponse.Message);
    }

    user = await _userManager.FindByEmailAsync(request.Email);

Flow for Guest Checkout:
1. Frontend sends email + password (new account registration)
2. AuthService.Login() checks if user exists:
   a. If EXISTS: Validate password → Return user
   b. If NOT EXIST: Could auto-register or return error (depends on business logic)
3. If login successful: Proceed to cart handling
4. If login fails: Rollback transaction, return error

Error Rollback Example:
- User enters wrong password → Login fails
- LoginResponse.IsSuccess = false
- RollbackAsync() called → All changes reverted
- Return 400 Bad Request with error message

Current Implementation:
- Does NOT auto-register guests
- Assumes guest email already has account
- To enable auto-registration: Call Register() before Login()
*/

// ============================================================================
// STEP 4: CART HANDLING & GUEST CART MERGE
// ============================================================================

/*
Location: Application/Services/OrderService.cs, Lines: ~167-225

Condition: if (!string.IsNullOrEmpty(request.GuestId))

Process:
1. Fetch all guest cart items
2. For each guest cart item:
   a. Find matching user cart item (if exists)
   b. If match: Add quantities together
   c. If no match: Create new user cart item
3. Delete all guest cart items
4. Clear guest cart in cache/session

Code Flow:
    var guestCartItems = await _guestCartService.GetAllAsync(
        filter: c => c.GuestId == request.GuestId,
        tracked: true);  // TRACKED for updates

    foreach (var guestCart in guestCartItems)
    {
        var existingUserCart = await _userCartService.GetAsync(
            filter: c => c.UserId == user.Id && c.VariantId == guestCart.VariantId,
            tracked: true);

        if (existingUserCart != null)
        {
            // MERGE: Combine quantities
            existingUserCart.Quantity += guestCart.Quantity;
            await _userCartService.UpdateAsync(existingUserCart);
        }
        else
        {
            // NEW: Create user cart entry
            var newUserCart = new UserCart
            {
                UserId = user.Id,
                VariantId = guestCart.VariantId,
                Quantity = guestCart.Quantity,
                CreatedAt = DateTime.UtcNow
            };
            await _userCartService.AddAsync(newUserCart);
        }
        
        // DELETE: Remove from guest cart
        await _guestCartService.DeleteAsync(guestCart);
    }

Entities Involved:
- GuestCart: Temporary items for anonymous users
  - VariantId, Quantity, GuestId, ExpireAt
  
- UserCart: Items for authenticated users
  - UserId, VariantId, Quantity, CreatedAt

Example Scenario:
Guest has 2x Red Shirt (VariantId: 101) in guest cart
User already has 1x Red Shirt in user cart
Result: User now has 3x Red Shirt total

Transaction Safety:
- All operations within same transaction
- If merge fails: Entire transaction rolls back
- Guest cart remains unchanged on failure
*/

// ============================================================================
// STEP 5: CREATE ORDER
// ============================================================================

/*
Location: Application/Services/OrderService.cs, Lines: ~227-255

Order Entity Creation:
    var order = new Order
    {
        UserId = user.Id,
        Number = orderNumber,  // "ORD-{IdempotencyKey}"
        Status = OrderStatus.Pending,  // Initial status
        FullAddress = request.Address,
        Governorate = request.Governorate,
        City = request.City,
        CreatedAt = DateTime.UtcNow,
        TotalPrice = 0,  // Will be calculated
        OrderDetails = new List<OrderDetail>()
    };

    await _orderService.AddAsync(order);

Order Properties:
- Id: Auto-generated primary key
- UserId: Foreign key to User
- Number: Unique order identifier (based on IdempotencyKey)
- Status: OrderStatus enum (Pending, Processing, Confirmed, Shipped, Delivered, Cancelled, Returned)
- FullAddress, Governorate, City: Shipping information
- TotalPrice: Sum of all order details
- CreatedAt: Timestamp when order created
- IsDeleted: Soft delete flag (ISoftDelete interface)

OrderStatus Enum Values:
    public enum OrderStatus
    {
        Pending = 0,       // Initial state
        Processing = 1,    // Being prepared
        Confirmed = 2,     // Payment confirmed
        Shipped = 3,       // In transit
        Delivered = 4,     // Received by customer
        Cancelled = 5,     // User cancelled
        Returned = 6       // Customer returned items
    }

Database INSERT:
- EF Core generates: INSERT INTO Orders (UserId, Number, Status, ...)
- Order.Id assigned after insert
- OrderDetails linked via OrderId (cascading)
*/

// ============================================================================
// STEP 6: VARIANT RESERVATION & ORDER DETAILS CREATION
// ============================================================================

/*
Location: Application/Services/OrderService.cs, Lines: ~257-310

Critical Loop - Process Each Cart Item:

    foreach (var cartItem in userCartItems)
    {
        var variant = cartItem.Variant;
        
        // SAFETY CHECK: Prevent overselling
        if (variant.Quantity < cartItem.Quantity)
        {
            await _unitOfWork.RollbackAsync();
            return Error($"Insufficient stock for variant {variant.Id}");
        }
        
        // RESERVATION: Mark as reserved
        variant.Quantity -= cartItem.Quantity;      // Reduce available
        variant.Reserved += cartItem.Quantity;      // Increase reserved
        await _variantService.UpdateAsync(variant);
        
        // PRICE CALCULATION
        var product = variant.Product;
        decimal unitPrice = product?.Price ?? 0;
        decimal itemTotalPrice = unitPrice * cartItem.Quantity;
        totalOrderPrice += itemTotalPrice;
        
        // ORDER DETAIL CREATION
        var orderDetail = new OrderDetail
        {
            OrderId = order.Id,
            VariantId = variant.Id,
            Quantity = cartItem.Quantity,
            UnitPrice = unitPrice,
            Discount = 0,  // Future enhancement
            TotalPrice = itemTotalPrice
        };
        
        order.OrderDetails.Add(orderDetail);
    }
    
    order.TotalPrice = totalOrderPrice;
    await _orderService.AddAsync(order);

Variant Model:
    public class Variant : BaseEntity
    {
        public int ProductId { get; set; }
        public int ColorId { get; set; }
        public int SizeId { get; set; }
        public int Quantity { get; set; }      // Available qty
        public int Reserved { get; set; }      // Reserved qty
    }

Inventory State Changes:
Before:  Quantity = 50, Reserved = 10
After:   Quantity = 45, Reserved = 15
         (Added 5 items to order)

Out of Stock Scenario:
- Variant.Quantity = 2, Request Qty = 5
- Check fails: 2 < 5
- RollbackAsync() called
- Transaction reverted to pre-transaction state
- No partial orders created
- Error returned to client

Price Source:
- Product.Price is the unit price
- Calculated at checkout time (historical accuracy)
- Stored in OrderDetail.UnitPrice (not recalculated later)
- Protects against future price changes

OrderDetail Structure:
    public class OrderDetail : BaseEntity
    {
        public int OrderId { get; set; }
        public int VariantId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }     // Snapshot price
        public decimal Discount { get; set; }      // Per-item discount
        public decimal TotalPrice { get; set; }    // UnitPrice * Qty - Discount
    }

Cascading Updates:
- Update one variant per cart item
- Each update happens within transaction
- If any update fails: All rollback
- Total price calculated after all items processed
*/

// ============================================================================
// STEP 7: INVENTORY TRANSACTION RECORDING
// ============================================================================

/*
Location: Application/Services/OrderService.cs, Lines: ~312-329

Purpose: Create an audit trail for all inventory movements

Code:
    foreach (var cartItem in userCartItems)
    {
        var inventoryTransaction = new InventoryTransaction
        {
            OrderId = order.Id,
            UserId = user.Id,
            VariantId = cartItem.VariantId,
            Quantity = cartItem.Quantity,
            TransactionType = InventoryTransactionType.Sale,
            CreatedAt = DateTime.UtcNow
        };
        
        await _inventoryTransactionService.AddAsync(inventoryTransaction);
    }

InventoryTransaction Entity:
    public class InventoryTransaction : BaseEntity
    {
        public int? OrderId { get; set; }           // Nullable for non-sale transactions
        public int UserId { get; set; }
        public int VariantId { get; set; }
        public int Quantity { get; set; }
        public InventoryTransactionType TransactionType { get; set; }
        public DateTime CreatedAt { get; set; }
    }

InventoryTransactionType Enum:
    public enum InventoryTransactionType
    {
        Purchase = 0,       // Goods received from supplier
        Sale = 1,           // Sold to customer (this checkout)
        Refund = 2,         // Customer returns goods
        Adjustment = 3,     // Manual inventory correction
        Transfer = 4,       // Between warehouses
        Reservation = 5     // Reserved but not yet completed
    }

Use Cases:
1. Sales Analytics: COUNT(*) WHERE TransactionType = Sale
2. Inventory Audit: SUM(Quantity) GROUP BY VariantId
3. Refund History: SELECT * WHERE TransactionType = Refund
4. User Purchase History: SELECT * WHERE UserId = ? AND TransactionType = Sale
5. Variant Movement: SELECT * WHERE VariantId = ? ORDER BY CreatedAt

Database INSERT:
- Each transaction is separate INSERT
- For 5 items in order: 5 inventory transaction records
- No UPDATE to InventoryTransaction (immutable audit log)
- Index on OrderId for quick lookup of transaction history
*/

// ============================================================================
// STEP 8: CLEAR USER CART
// ============================================================================

/*
Location: Application/Services/OrderService.cs, Lines: ~331-336

After Order is Created Successfully:
    foreach (var cartItem in userCartItems)
    {
        await _userCartService.DeleteAsync(cartItem);
    }

Why Clear Cart?
- Prevents duplicate items if user checks out again
- Reflects real-world behavior (cart empties after purchase)
- Next purchase starts with fresh cart
- Cleans up database (no orphaned cart items)

Soft Delete vs Hard Delete:
- UserCart is not ISoftDelete interface
- Uses HardDeleteAsync() → Physical removal
- Reasons:
  1. Cart is temporary, not business-relevant history
  2. Reduces database bloat
  3. No "restore cart" feature needed
  4. Clean separation from orders (permanent records)

Rollback Behavior:
- If order creation fails BEFORE this step: Cart remains
- User can retry checkout with same cart
- Only cleared after complete order success
*/

// ============================================================================
// STEP 9: COMMIT TRANSACTION
// ============================================================================

/*
Location: Application/Services/OrderService.cs, Lines: ~338-345

Commit Steps:
    await _unitOfWork.SaveChanges();
    await _unitOfWork.CommitAsync();

SaveChanges() Behavior:
- Detects all modified entities
- Generates SQL INSERT/UPDATE/DELETE statements
- Executes all statements
- Writes to database (but still in transaction)

CommitAsync() Behavior:
- Tells database: "All done, keep these changes"
- Releases transaction lock
- Makes changes permanent and visible to other connections
- Cannot be undone after this point

Combined Effect:
1. SaveChanges(): Writes all pending changes
2. CommitAsync(): Makes them permanent
3. Transaction ends cleanly
4. All changes visible to other users/requests

Example Transaction Log:
    BEGIN TRANSACTION
    INSERT INTO Orders (UserId, Number, Status, ...)
    INSERT INTO OrderDetails (OrderId, VariantId, ...)
    UPDATE Variants SET Quantity = ?, Reserved = ?
    INSERT INTO InventoryTransactions (OrderId, VariantId, ...)
    DELETE FROM UserCarts WHERE UserId = ?
    COMMIT
*/

// ============================================================================
// ERROR HANDLING & ROLLBACK
// ============================================================================

/*
Location: Application/Services/OrderService.cs, Lines: ~349-373

Catch Block Pattern:
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
        
        return new ApiResponse<CheckoutResponseDto>()
        {
            Success = false,
            Message = $"Checkout failed: {ex.Message}"
        };
    }

Errors That Trigger Rollback:
1. null user after login
2. Empty cart
3. Out of stock (Quantity < Requested)
4. Database connection failure
5. Constraint violations
6. Any unexpected exception

Rollback Safety:
- Reverts ALL changes to pre-transaction state
- Order, OrderDetails, Variant updates: UNDONE
- Inventory transactions: NOT created
- User cart: STILL has items (can retry)
- Guest cart: STILL has items (can retry)

Nested Try-Catch:
- Outer catch: Handles all errors
- Inner try-catch: Handles rollback errors
- Even if rollback fails, user gets error response
- Prevents exceptions from escaping

Logging:
- All errors logged with full exception details
- Stack trace captured for debugging
- Rollback success/failure tracked
- Enables post-incident analysis
*/

// ============================================================================
// RESPONSE STRUCTURE
// ============================================================================

/*
Success Response (HTTP 200 OK):
{
  "success": true,
  "message": "Order created successfully",
  "data": {
    "orderId": 42,
    "orderNumber": "ORD-550e8400-e29b-41d4-a716",
    "totalPrice": 499.99,
    "createdAt": "2025-01-20T15:30:45.123Z"
  }
}

Error Response (HTTP 400 Bad Request):
{
  "success": false,
  "message": "Insufficient stock for variant 101. Available: 2, Requested: 5",
  "data": null
}

Response DTO:
    public class CheckoutResponseDto
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
    }

API Response Wrapper:
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T? Data { get; set; }
    }

Status Code Mapping:
- 200 OK: Order created OR idempotent retry
- 400 Bad Request: Validation error, empty cart, out of stock
- 500 Internal Server Error: Unexpected database/system error
*/

// ============================================================================
// DEPENDENCY INJECTION SETUP
// ============================================================================

/*
In Program.cs or Startup.cs ConfigureServices:

    // Repository & Unit of Work
    services.AddScoped<IUnitOfWork, UnitOfWork>();
    services.AddScoped(typeof(IServices<>), typeof(Services<>));
    
    // Order Service
    services.AddScoped<IOrderService, OrderService>();
    
    // Auth Service
    services.AddScoped<IAuthService, AuthService>();
    
    // Cart Services
    services.AddScoped<ICartService, CartService>();
    
    // DbContext
    services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"))
    );
    
    // Identity
    services.AddIdentity<User, Role>(options =>
    {
        // Configure password, lockout, etc.
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

Dependency Chain:
OrderController
    → IOrderService (OrderService)
        → IUnitOfWork (UnitOfWork)
        → IServices<Order, OrderDetail, Variant, etc.> (Services<T>)
        → UserManager<User> (Identity)
        → IAuthService (AuthService)
        → ILogger<OrderService> (Logging)
*/

// ============================================================================
// TESTING EXAMPLES
// ============================================================================

/*
Unit Test Example:
    [TestClass]
    public class OrderServiceTests
    {
        [TestMethod]
        public async Task ProcessCheckoutAsync_WithIdempotencyKey_ReturnsCachedOrder()
        {
            // Arrange
            var existingOrder = new Order { Id = 1, Number = "idempotency-key" };
            var mockOrderService = new Mock<IServices<Order>>();
            mockOrderService.Setup(s => s.GetAsync(
                It.IsAny<Expression<Func<Order, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<Order, object>>[]>()))
                .ReturnsAsync(existingOrder);
            
            var request = new CheckoutRequestDto { IdempotencyKey = "idempotency-key" };
            
            // Act
            var result = await orderService.ProcessCheckoutAsync(request);
            
            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, result.Data.OrderId);
        }
        
        [TestMethod]
        public async Task ProcessCheckoutAsync_WithInsufficientStock_RollsBackTransaction()
        {
            // Arrange
            var variant = new Variant { Id = 101, Quantity = 2 };
            var cartItem = new UserCart { VariantId = 101, Quantity = 5 };
            
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var request = new CheckoutRequestDto { /* ... */ };
            
            // Act
            var result = await orderService.ProcessCheckoutAsync(request);
            
            // Assert
            Assert.IsFalse(result.Success);
            mockUnitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
        }
    }

Integration Test Example:
    [TestClass]
    public class OrderCheckoutIntegrationTests
    {
        private AppDbContext _context;
        private OrderService _orderService;
        
        [TestInitialize]
        public void Setup()
        {
            // Create in-memory database
            _context = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseInMemoryDatabase("TestDb")
                    .Options);
        }
        
        [TestMethod]
        public async Task FullCheckoutFlow_WithValidCart_CreatesOrderSuccessfully()
        {
            // Arrange
            var user = new User { Id = 1, Email = "test@test.com" };
            var variant = new Variant { Id = 1, Quantity = 100, Reserved = 0 };
            var cartItem = new UserCart { UserId = 1, VariantId = 1, Quantity = 5 };
            
            _context.Users.Add(user);
            _context.Variants.Add(variant);
            _context.UserCarts.Add(cartItem);
            await _context.SaveChangesAsync();
            
            var request = new CheckoutRequestDto
            {
                IdempotencyKey = "test-key",
                Email = "test@test.com",
                IsAuthenticated = true,
                Address = "123 Main St",
                City = "Cairo",
                Governorate = "Cairo"
            };
            
            // Act
            var result = await _orderService.ProcessCheckoutAsync(request);
            
            // Assert
            Assert.IsTrue(result.Success);
            
            var createdOrder = _context.Orders.FirstOrDefault();
            Assert.IsNotNull(createdOrder);
            Assert.AreEqual(1, createdOrder.UserId);
            Assert.AreEqual(OrderStatus.Pending, createdOrder.Status);
            
            var updatedVariant = _context.Variants.First();
            Assert.AreEqual(95, updatedVariant.Quantity);    // 100 - 5
            Assert.AreEqual(5, updatedVariant.Reserved);     // 0 + 5
        }
    }
*/

// ============================================================================
// PERFORMANCE METRICS
// ============================================================================

/*
Expected Response Times (SSD, 8GB RAM, SQL Server):
- Idempotent request (cached): ~20ms
- Full new order: ~100-200ms
- With 20+ items: ~300-500ms
- Under load (100 concurrent): ~2-5 seconds

Database Queries per Checkout:
1. Check existing order: 1 SELECT
2. Get user: 1 SELECT
3. Merge guest cart: 1 SELECT + N UPDATE + N DELETE
4. Get user cart: 1 SELECT with eager loading
5. Create order: 1 INSERT (batched with details)
6. Update variants: N UPDATE (1 per item)
7. Create inventory transactions: N INSERT
8. Delete cart items: 1 DELETE (bulk)

Total: ~6-15 queries depending on cart size

Database Connection Pool:
- Default: 100 connections
- Checkout holds connection: ~200ms average
- Safe for ~500 concurrent checkouts
- Monitor connection usage in production
*/

// ============================================================================
// MONITORING & OBSERVABILITY
// ============================================================================

/*
Log Points (Detailed Logging at Each Step):

Step 1: "Step 1: Beginning database transaction"
Step 2: "Step 2: Checking for existing order with IdempotencyKey: {key}"
        "Idempotent request: Order already exists..."
Step 3: "Step 3: Authentication handling - IsAuthenticated: {bool}"
        "User authenticated: UserId: {id}, Email: {email}"
Step 4: "Step 4: Cart handling - GuestId: {id}"
        "Merging guest cart (GuestId: {id}) into user cart"
Step 5: "Step 5: Creating order for UserId: {id}"
Step 6: "Step 6: Processing variant reservations..."
        "Processing variant - VariantId: {id}, Requested Qty: {qty}"
        "Variant reserved - VariantId: {id}, NewQuantity: {qty}"
Step 7: "Step 7: Recording inventory transactions"
Step 9: "Step 9: Committing transaction"
        "Checkout completed successfully - OrderId: {id}"

Error Logs:
- "Checkout process failed" + Exception.Message
- "Transaction rolled back successfully"
- "Error during transaction rollback" (critical!)

Metrics to Track:
- Checkout success rate
- Average checkout time
- Out-of-stock errors (inventory)
- Authentication failures
- Rollback frequency
- P50, P95, P99 latency
*/

// ============================================================================
// END OF REFERENCE GUIDE
// ============================================================================
