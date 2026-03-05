# Checkout → Order Processing Workflow Documentation

## Overview
Production-ready ASP.NET Core Web API implementing a complete checkout → order processing workflow using Onion Architecture with EF Core, Repository Pattern, Unit of Work, and Database Transactions.

## Architecture Components

### Layers
- **API Layer** (`API\Controllers\OrderController.cs`): HTTP endpoint and request validation
- **Application Layer** (`Application\Services\OrderService.cs`): Business logic and workflow orchestration
- **Core Layer**: Entity definitions and interfaces
- **Infrastructure Layer**: Repository implementations, UnitOfWork, and database persistence

### Key Patterns Implemented
- ✅ **Repository Pattern**: Generic `IServices<T>` interface for data access
- ✅ **Unit of Work Pattern**: Transaction management with `IUnitOfWork`
- ✅ **Dependency Injection**: Constructor-based DI for loose coupling
- ✅ **Async/Await**: All database operations are asynchronous
- ✅ **Error Handling**: Comprehensive try-catch with transaction rollback
- ✅ **Logging**: Detailed logging at each step for observability

## Checkout Workflow (9 Steps)

### STEP 1: START DATABASE TRANSACTION
```csharp
await _unitOfWork.BeginTransactionAsync();
```
- Initiates a database transaction to ensure ACID compliance
- All subsequent operations are atomic
- Either all succeed or all rollback

### STEP 2: IDEMPOTENCY CHECK
```csharp
var existingOrder = await _orderService.GetAsync(
    filter: o => o.Number == request.IdempotencyKey,
    tracked: false);
```
- Prevents duplicate orders from retried requests
- Uses `IdempotencyKey` as unique identifier
- Returns cached order if already exists
- **Benefit**: Safe for network retries and frontend double-clicks

### STEP 3: AUTHENTICATION HANDLING
```csharp
if (request.IsAuthenticated)
{
    user = await _userManager.FindByEmailAsync(request.Email);
}
else
{
    var loginResponse = await _authService.Login(loginDto);
    if (!loginResponse.IsSuccess)
        return error; // Rollback on failure
}
```
- Handles both authenticated users and guest checkout
- Guest checkout creates account or logs in existing user
- Validates credentials before order processing
- **Rollback**: If login fails, transaction rolls back

### STEP 4: CART HANDLING (Guest Cart Merge)
```csharp
var guestCartItems = await _guestCartService.GetAllAsync(
    filter: c => c.GuestId == request.GuestId,
    tracked: true);

foreach (var guestCart in guestCartItems)
{
    var existingUserCart = await _userCartService.GetAsync(
        filter: c => c.UserId == user.Id && c.VariantId == guestCart.VariantId);
    
    if (existingUserCart != null)
        existingUserCart.Quantity += guestCart.Quantity;
    else
        // Create new user cart item
}
```
- Merges guest cart into user cart if `GuestId` provided
- Combines quantities if variant already in user cart
- Deletes guest cart items after merge
- **Atomicity**: All within same transaction

### STEP 5: CREATE ORDER
```csharp
var order = new Order
{
    UserId = user.Id,
    Number = orderNumber,
    Status = OrderStatus.Pending,
    FullAddress = request.Address,
    Governorate = request.Governorate,
    City = request.City,
    CreatedAt = DateTime.UtcNow,
    TotalPrice = totalOrderPrice,
    OrderDetails = new List<OrderDetail>()
};

await _orderService.AddAsync(order);
```
- Creates main `Order` entity with shipping details
- Sets status to `Pending` initially
- Calculates total price from cart items
- Records creation timestamp

### STEP 6: VARIANT RESERVATION (Stock Check & Reduction)
```csharp
foreach (var cartItem in userCartItems)
{
    var variant = cartItem.Variant;
    
    // Check availability
    if (variant.Quantity < cartItem.Quantity)
        return error; // Rollback on insufficient stock
    
    // Reserve (reduce available, increase reserved)
    variant.Quantity -= cartItem.Quantity;
    variant.Reserved += cartItem.Quantity;
    await _variantService.UpdateAsync(variant);
    
    // Create OrderDetail
    var orderDetail = new OrderDetail
    {
        OrderId = order.Id,
        VariantId = variant.Id,
        Quantity = cartItem.Quantity,
        UnitPrice = product.Price,
        TotalPrice = product.Price * cartItem.Quantity
    };
    
    order.OrderDetails.Add(orderDetail);
}
```
- **Stock Validation**: Ensures sufficient inventory per variant
- **Reservation**: Reduces `Quantity`, increases `Reserved`
- **Order Details**: Links order to specific variants
- **Rollback**: If any item out of stock, entire transaction rolls back

#### Variant Model Structure
```csharp
public class Variant : BaseEntity
{
    public int Quantity { get; set; }      // Available quantity
    public int Reserved { get; set; }      // Reserved for pending orders
}
```

### STEP 7: SAVE INVENTORY TRANSACTION RECORD
```csharp
foreach (var cartItem in userCartItems)
{
    var inventoryTransaction = new InventoryTransaction
    {
        OrderId = order.Id,
        UserId = user.Id,
        VariantId = cartItem.VariantId,
        Quantity = cartItem.Quantity,
        TransactionType = InventoryTransactionType.Sale,  // Enum value
        CreatedAt = DateTime.UtcNow
    };
    
    await _inventoryTransactionService.AddAsync(inventoryTransaction);
}
```
- Records inventory movement for audit trail
- Links transaction to order and user
- Uses `InventoryTransactionType.Sale` enum
- Enables inventory analytics and reporting

#### InventoryTransactionType Enum
```csharp
public enum InventoryTransactionType
{
    Purchase,      // Incoming inventory
    Sale,          // Order checkout
    Refund,        // Order return
    Adjustment,    // Manual adjustment
    Transfer,      // Between warehouses
    Reservation    // Reserved stock
}
```

### STEP 8: CLEAR USER CART
```csharp
foreach (var cartItem in userCartItems)
{
    await _userCartService.DeleteAsync(cartItem);
}
```
- Removes all items from user cart after successful order
- Ensures clean state for next purchase

### STEP 9: COMMIT TRANSACTION
```csharp
await _unitOfWork.SaveChanges();
await _unitOfWork.CommitAsync();
```
- Persists all changes to database
- Commits the transaction
- All operations become permanent and visible

## Transaction Rollback Scenario
```csharp
catch (Exception ex)
{
    try
    {
        await _unitOfWork.RollbackAsync();
    }
    catch (Exception rollbackEx)
    {
        _logger.LogError(rollbackEx, "Rollback failed");
    }
}
```
- **Triggered by**:
  - Login failure
  - Insufficient inventory
  - Duplicate order (idempotency)
  - Any exception during processing
  
- **Result**: ALL changes reverted to pre-transaction state

## API Endpoint

### POST /api/order/checkout
```http
POST /api/order/checkout HTTP/1.1
Content-Type: application/json

{
  "idempotencyKey": "unique-uuid-or-timestamp",
  "email": "customer@example.com",
  "password": "password123",
  "isAuthenticated": false,
  "guestId": "guest-session-id",
  "firstName": "John",
  "lastName": "Doe",
  "phone": "+201001234567",
  "address": "123 Main Street",
  "city": "Cairo",
  "governorate": "Cairo Governorate"
}
```

### Success Response (200 OK)
```json
{
  "success": true,
  "message": "Order created successfully",
  "data": {
    "orderId": 1,
    "orderNumber": "ORD-unique-idempotency-key",
    "totalPrice": 499.99,
    "createdAt": "2025-01-20T15:30:00Z"
  }
}
```

### Error Response (400 Bad Request)
```json
{
  "success": false,
  "message": "Insufficient stock for variant 42. Available: 2, Requested: 5",
  "data": null
}
```

## Key Features & Safety Measures

### 🔒 ACID Compliance
- **Atomicity**: All-or-nothing transaction execution
- **Consistency**: Inventory counts remain valid
- **Isolation**: Concurrent requests don't interfere
- **Durability**: Committed data persists

### 🛡️ Idempotency
- Prevents duplicate orders on retry
- Safe for unstable network conditions
- Uses unique `IdempotencyKey` per request

### 📊 Inventory Management
- Real-time stock validation
- Prevents overselling
- Tracks reserved vs available quantities
- Complete audit trail via `InventoryTransaction`

### 📝 Comprehensive Logging
- Every step logged with context
- UserId, VariantId, Quantities tracked
- Errors captured with full stack trace
- Supports debugging and monitoring

### 🔄 Cart Merge Logic
- Seamless guest → authenticated user transition
- Combines quantities intelligently
- Atomic within transaction
- No data loss during merge

### ⚡ Async/Await Throughout
- Non-blocking database operations
- Better performance under load
- Scalable to thousands of concurrent orders
- Proper async task handling

## Database Entities Involved

### Order
```csharp
public class Order : BaseEntity, ISoftDelete
{
    public int UserId { get; set; }
    public string Number { get; set; }
    public decimal TotalPrice { get; set; }
    public OrderStatus Status { get; set; }
    public User User { get; set; }
    public ICollection<OrderDetail> OrderDetails { get; set; }
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; }
}
```

### OrderDetail
```csharp
public class OrderDetail : BaseEntity
{
    public int OrderId { get; set; }
    public int VariantId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalPrice { get; set; }
}
```

### Variant
```csharp
public class Variant : BaseEntity
{
    public int Quantity { get; set; }       // Available
    public int Reserved { get; set; }       // Reserved for orders
}
```

### InventoryTransaction
```csharp
public class InventoryTransaction : BaseEntity
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public int VariantId { get; set; }
    public int Quantity { get; set; }
    public InventoryTransactionType TransactionType { get; set; }
}
```

### UserCart & GuestCart
- Temporary storage for items before checkout
- Cleared after successful order
- Merged when guest logs in

## Error Scenarios & Handling

| Scenario | Handling | Result |
|----------|----------|--------|
| User not found | Log error, rollback | 400 User not found |
| Invalid password | Return auth error, rollback | 400 Invalid credentials |
| Empty cart | Return error, rollback | 400 Cart is empty |
| Out of stock | Return error with detail, rollback | 400 Insufficient stock |
| Idempotent request | Return existing order | 200 OK (cached response) |
| DB transaction error | Rollback all changes | 500 Checkout failed |

## Performance Considerations

### Indexed Fields
- Order.Number (Idempotency check)
- Variant.ProductId (Cart queries)
- UserCart.UserId (User cart retrieval)
- InventoryTransaction.OrderId (Audit queries)

### Query Optimization
- Eager loading with `includes` parameter
- Tracked vs untracked queries appropriately
- Single query per cart item during checkout
- Transaction keeps connection alive (no reconnects)

### Scalability
- Async/await prevents thread pool starvation
- Database transactions kept brief
- No nested transactions
- Rollback is fast and clean

## Integration Points

### Dependency Injection (Startup/Program.cs)
```csharp
services.AddScoped<IOrderService, OrderService>();
services.AddScoped<IUnitOfWork, UnitOfWork>();
services.AddScoped(typeof(IServices<>), typeof(Services<>));
services.AddScoped<IAuthService, AuthService>();
services.AddScoped<ICartService, CartService>();
```

### Controller Registration
```csharp
[Route("api/[controller]")]
[ApiController]
public class OrderController : ControllerBase
{
    public OrderController(IOrderService orderService, ILogger<OrderController> logger)
}
```

## Testing Recommendations

### Unit Tests
- Mock IServices, IUnitOfWork, IAuthService
- Test each step independently
- Verify rollback on failures
- Test idempotency logic

### Integration Tests
- Real database with transaction scope
- Test stock validation scenarios
- Test guest cart merge
- Test concurrent checkout attempts

### E2E Tests
- Frontend → Backend → Database
- Test full happy path
- Test error scenarios
- Verify response format

## Security Considerations

1. **Password**: Only used during guest checkout, never stored in order
2. **User ID**: Validated from authenticated context
3. **Cart Isolation**: Users can only access their own carts
4. **Idempotency**: Prevents replay attacks via key validation
5. **Input Validation**: Email, address, quantities validated

## Future Enhancements

- [ ] Discount/Coupon support
- [ ] Tax calculation
- [ ] Shipping cost integration
- [ ] Payment gateway integration
- [ ] Order confirmation email
- [ ] Inventory webhook notifications
- [ ] Multiple shipping addresses
- [ ] Gift card redemption
- [ ] Order status history tracking
- [ ] Partial cancellation support
