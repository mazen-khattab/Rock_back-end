# ✅ Checkout Implementation - Developer Checklist

## Pre-Deployment Verification

### Code Review Checklist
- [ ] Read through `OrderService.cs` to understand workflow
- [ ] Review `OrderController.cs` endpoint implementation
- [ ] Verify `CheckoutResponseDto.cs` exists and is imported
- [ ] Check all using statements are correct
- [ ] Verify no hardcoded values or test data remains
- [ ] Ensure ILogger<T> is used consistently
- [ ] Check for SQL injection vulnerabilities (all parameterized)
- [ ] Verify async/await properly used (no blocking calls)

### Architecture Verification
- [ ] UnitOfWork pattern implemented (BeginTransactionAsync, CommitAsync, RollbackAsync)
- [ ] Repository pattern used (IServices<T> for all data access)
- [ ] Dependency injection configured (all services injected)
- [ ] Clean separation of concerns (API → Service → Data Access)
- [ ] Error handling with proper rollback
- [ ] Logging at key points

### Database Schema Verification
- [ ] Order table exists with all required fields
- [ ] OrderDetail table exists and linked to Order
- [ ] Variant table has Quantity and Reserved fields
- [ ] InventoryTransaction table exists
- [ ] UserCart table exists and can be cleared
- [ ] GuestCart table exists for merging
- [ ] User table linked correctly
- [ ] All foreign keys are configured
- [ ] Indices exist on frequently queried fields

### Entity Model Verification
```
Order:
  ├─ Id (int, PK) ✓
  ├─ UserId (int, FK → User) ✓
  ├─ Number (string, unique for idempotency) ✓
  ├─ TotalPrice (decimal) ✓
  ├─ Status (OrderStatus enum) ✓
  ├─ FullAddress, Governorate, City (string) ✓
  ├─ CreatedAt (DateTime) ✓
  └─ OrderDetails, InventoryTransactions (navigation) ✓

OrderDetail:
  ├─ Id (int, PK) ✓
  ├─ OrderId (int, FK) ✓
  ├─ VariantId (int, FK) ✓
  ├─ Quantity, UnitPrice, Discount, TotalPrice ✓
  └─ Variant (navigation) ✓

Variant:
  ├─ Id (int, PK) ✓
  ├─ ProductId (int, FK) ✓
  ├─ Quantity (int, available) ✓
  ├─ Reserved (int, reserved for orders) ✓
  └─ Product (navigation with Price field) ✓
```

### Enum Verification
```
OrderStatus:
  ├─ Pending ✓
  ├─ Processing ✓
  ├─ Confirmed ✓
  ├─ Shipped ✓
  ├─ Delivered ✓
  ├─ Cancelled ✓
  └─ Returned ✓

InventoryTransactionType:
  ├─ Purchase ✓
  ├─ Sale ← Used in checkout ✓
  ├─ Refund ✓
  ├─ Adjustment ✓
  ├─ Transfer ✓
  └─ Reservation ✓
```

### DTO Verification
```
CheckoutRequestDto:
  ├─ idempotencyKey (string) ✓
  ├─ email (string) ✓
  ├─ password (string, nullable) ✓
  ├─ isAuthenticated (bool) ✓
  ├─ guestId (string, nullable) ✓
  ├─ firstName, lastName (string) ✓
  ├─ phone (string) ✓
  ├─ address (string) ✓
  ├─ city (string) ✓
  └─ governorate (string) ✓

CheckoutResponseDto:
  ├─ OrderId (int) ✓
  ├─ OrderNumber (string) ✓
  ├─ TotalPrice (decimal) ✓
  └─ CreatedAt (DateTime) ✓

ApiResponse<T>:
  ├─ Success (bool) ✓
  ├─ Message (string) ✓
  └─ Data (T or null) ✓
```

## Dependency Injection Setup

### Program.cs/Startup.cs Configuration
```csharp
// ✓ OrderService registration
services.AddScoped<IOrderService, OrderService>();

// ✓ UnitOfWork registration (should already exist)
services.AddScoped<IUnitOfWork, UnitOfWork>();

// ✓ Generic repository registration (should already exist)
services.AddScoped(typeof(IServices<>), typeof(Services<>));

// ✓ AuthService registration (should already exist)
services.AddScoped<IAuthService, AuthService>();

// ✓ DbContext registration (should already exist)
services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"))
);

// ✓ Identity configuration (should already exist)
services.AddIdentity<User, Role>(/* config */)
    .AddEntityFrameworkStores<AppDbContext>();

// ✓ Logging configuration
services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
    config.SetMinimumLevel(LogLevel.Information);
});
```

- [ ] IOrderService registered
- [ ] IUnitOfWork available
- [ ] IServices<T> available
- [ ] IAuthService available
- [ ] AppDbContext configured
- [ ] Identity configured
- [ ] Logging configured

## Build & Compilation

### Initial Build
```bash
dotnet build
```
- [ ] Build succeeds with no errors
- [ ] No compilation warnings
- [ ] All dependencies resolved
- [ ] NuGet packages correct version

### Specific File Compilation
```bash
dotnet build --include-source
```
- [ ] OrderService.cs compiles ✓
- [ ] OrderController.cs compiles ✓
- [ ] CheckoutResponseDto.cs compiles ✓
- [ ] IOrderService.cs compiles ✓

## Database Setup

### Connection String
- [ ] SQL Server instance accessible
- [ ] Connection string configured in appsettings.json
- [ ] Credentials correct (if not Windows Auth)
- [ ] Database exists or creates on first migration

### Migrations
```bash
dotnet ef migrations add AddCheckoutWorkflow
dotnet ef database update
```
- [ ] Migrations applied successfully
- [ ] All tables created
- [ ] Foreign keys configured
- [ ] Indices created
- [ ] Seed data applied (if any)

### Database Verification
```sql
-- Verify tables exist
SELECT * FROM INFORMATION_SCHEMA.TABLES;

-- Verify key tables
SELECT * FROM Orders;
SELECT * FROM OrderDetails;
SELECT * FROM Variants;
SELECT * FROM InventoryTransactions;
SELECT * FROM UserCarts;
```

- [ ] Orders table exists and is empty initially
- [ ] OrderDetails table exists
- [ ] Variants table has test data
- [ ] Users table has test user
- [ ] UserCarts table has test items

## Testing

### Unit Testing
- [ ] Create unit test file: `Tests/OrderServiceTests.cs`
- [ ] Test idempotency check logic
- [ ] Test stock validation (sufficient/insufficient)
- [ ] Test cart merge logic
- [ ] Test error scenarios
- [ ] Test transaction rollback simulation

### Integration Testing
```bash
dotnet test
```
- [ ] All tests pass
- [ ] Database interactions work
- [ ] Transactions commit/rollback correctly
- [ ] No orphaned data after rollback

### Manual Testing

#### Test Case 1: Successful New Order
```
POST /api/order/checkout
{
  "idempotencyKey": "test-123",
  "email": "test@example.com",
  "password": "password",
  "isAuthenticated": false,
  "guestId": null,
  "firstName": "John",
  "lastName": "Doe",
  "phone": "+201001234567",
  "address": "123 Main St",
  "city": "Cairo",
  "governorate": "Cairo"
}

Expected: 200 OK
{
  "success": true,
  "message": "Order created successfully",
  "data": {
    "orderId": 1,
    "orderNumber": "ORD-test-123",
    "totalPrice": 299.99,
    "createdAt": "2025-01-20T15:30:00Z"
  }
}

Database Verification:
  ✓ SELECT * FROM Orders WHERE Id = 1; → Found
  ✓ Variant.Quantity decreased ✓
  ✓ Variant.Reserved increased ✓
  ✓ InventoryTransaction created ✓
```
- [ ] Response 200 OK
- [ ] Order created in database
- [ ] OrderDetails created
- [ ] Variants reserved
- [ ] InventoryTransactions recorded
- [ ] Cart cleared

#### Test Case 2: Idempotency Retry
```
Same request as Test Case 1 (same idempotencyKey)

Expected: 200 OK (same OrderId as Test Case 1, NO duplicate)
{
  "success": true,
  "message": "Order already exists (Idempotent request)",
  "data": {
    "orderId": 1,  ← SAME as Test Case 1
    ...
  }
}
```
- [ ] Same OrderId returned
- [ ] No new order created
- [ ] Database shows only one order

#### Test Case 3: Insufficient Stock
```
Setup: Variant with Quantity = 2
Request: Quantity = 5

Expected: 400 Bad Request
{
  "success": false,
  "message": "Insufficient stock for variant X. Available: 2, Requested: 5",
  "data": null
}

Database Verification:
  ✓ No order created ✓
  ✓ Variant unchanged ✓
  ✓ Cart NOT cleared ✓
```
- [ ] Response 400 Bad Request
- [ ] No order created
- [ ] No variant changes
- [ ] Cart items remain
- [ ] Transaction rolled back

#### Test Case 4: Invalid Credentials
```
Request: isAuthenticated=false, email=nonexistent@example.com, password=wrong

Expected: 400 Bad Request
{
  "success": false,
  "message": "Invalid Cedentials.",
  "data": null
}
```
- [ ] Response 400 Bad Request
- [ ] No order created
- [ ] Error message clear

#### Test Case 5: Guest Cart Merge
```
Setup: 
  - GuestCart: 2x Item A, 1x Item B
  - UserCart: 1x Item A (after user logs in)

Request: guestId provided

Expected: Merged cart
  - UserCart: 3x Item A (2+1), 1x Item B
  - GuestCart: EMPTY
```
- [ ] Guest cart items merged
- [ ] Quantities combined
- [ ] Guest cart cleared
- [ ] Order created with merged items

### Load Testing
```
Apache JMeter or similar tool
- 100 concurrent checkout requests
- Duration: 1 minute
- Expected: 99%+ success rate
- Response time P95: < 500ms
```

- [ ] No overselling under concurrent load
- [ ] Response times acceptable
- [ ] Database connection pool sufficient
- [ ] No deadlocks or lock timeouts

## Documentation Verification

- [ ] CHECKOUT_IMPLEMENTATION_GUIDE.md readable
- [ ] CHECKOUT_CODE_REFERENCE.cs explains all steps
- [ ] CHECKOUT_QUICKSTART.md has correct instructions
- [ ] CHECKOUT_API_DOCUMENTATION.md complete
- [ ] CHECKOUT_WORKFLOW_VISUAL.txt displays correctly
- [ ] IMPLEMENTATION_SUMMARY.md has checklist items

## Performance Profiling

### Baseline Metrics
```
Endpoint: POST /api/order/checkout

Scenario 1: New Order (5 items)
  Expected Time: 150-300ms
  Max Time: 500ms

Scenario 2: Idempotent Retry
  Expected Time: ~20ms
  Max Time: 100ms

Scenario 3: Out of Stock
  Expected Time: ~100ms
  Max Time: 200ms
```

- [ ] Measure actual response times
- [ ] Profile database queries (SQL Server Profiler)
- [ ] Check transaction lock times
- [ ] Monitor memory usage
- [ ] Verify connection pool usage
- [ ] Identify slow queries (if any)

## Security Verification

### Input Validation
- [ ] Empty idempotencyKey rejected (400)
- [ ] Empty email rejected (400)
- [ ] Missing address rejected (400)
- [ ] Special characters handled safely
- [ ] SQL injection attempts fail (parameterized queries)

### Authentication
- [ ] Password not logged anywhere
- [ ] Password not stored in Order/OrderDetail
- [ ] Only used during authentication
- [ ] Session management correct

### Authorization
- [ ] Users can't see other users' orders
- [ ] Users can't modify other users' carts
- [ ] Admin-only endpoints protected (if any)

### Data Protection
- [ ] Sensitive data not logged
- [ ] HTTPS/TLS enforced (production)
- [ ] Connection string not in code
- [ ] Credentials from environment variables

## Error Monitoring Setup

### Application Insights / Sentry Configuration
```csharp
// In Program.cs
builder.Services.AddApplicationInsightsTelemetry();
// OR
builder.Services.AddSentry(options => { ... });
```

- [ ] Error tracking configured
- [ ] Exceptions logged automatically
- [ ] Response times tracked
- [ ] Alerts configured for errors
- [ ] Dashboard accessible

## Logging Verification

Check logs for:
```
info: OrderService[0] Checkout process started with IdempotencyKey: {key}
info: OrderService[0] Step 1: Beginning database transaction
info: OrderService[0] Step 2: Checking for existing order...
info: OrderService[0] Step 3: Authentication handling...
info: OrderService[0] User authenticated: UserId: {id}
info: OrderService[0] Step 4: Cart handling...
info: OrderService[0] Step 5: Creating order...
info: OrderService[0] Step 6: Processing variant reservations...
info: OrderService[0] Step 7: Recording inventory transactions...
info: OrderService[0] Step 8: Clearing user cart...
info: OrderService[0] Step 9: Committing transaction...
info: OrderService[0] Checkout completed successfully
```

- [ ] Logs appear at each step
- [ ] Timestamps are correct
- [ ] User IDs logged
- [ ] Error logs are detailed
- [ ] Sensitive data not logged

## Post-Deployment Monitoring

### Application Health Checks
- [ ] Endpoint responds to requests
- [ ] Database connection stable
- [ ] No error rates above baseline
- [ ] Response times consistent
- [ ] Transaction success rate 99%+

### Data Integrity Checks
```sql
-- Daily check
SELECT COUNT(*) as TotalOrders FROM Orders;
SELECT COUNT(*) as OrphanedOrderDetails 
  FROM OrderDetails 
  WHERE OrderId NOT IN (SELECT Id FROM Orders);
SELECT COUNT(*) as InventoryMismatch 
  FROM Variants v
  WHERE (SELECT SUM(Reserved) FROM Variants WHERE ProductId = v.ProductId) 
        > (SELECT Quantity + Reserved FROM Variants WHERE ProductId = v.ProductId);
```

- [ ] No orphaned OrderDetails
- [ ] No inventory inconsistencies
- [ ] Order counts match expectations
- [ ] InventoryTransaction audit trail intact

### User Experience Checks
- [ ] Checkout completes successfully
- [ ] Order confirmation email sent
- [ ] Order appears in user account
- [ ] Can't re-checkout same items
- [ ] Can retry if checkout fails

## Rollback Plan

If issues found in production:
1. [ ] Identify root cause
2. [ ] Create hotfix
3. [ ] Build & test locally
4. [ ] Deploy hotfix
5. [ ] Monitor for improvement
6. [ ] Document incident
7. [ ] Update documentation

## Sign-Off Checklist

### Developer Sign-Off
- [ ] Code reviewed and approved
- [ ] All tests passing
- [ ] Documentation complete
- [ ] Performance acceptable
- [ ] No security vulnerabilities
- [ ] Ready for QA

### QA Sign-Off
- [ ] Manual testing complete
- [ ] Load testing passed
- [ ] Error scenarios verified
- [ ] Integration with other services tested
- [ ] Ready for production

### Operations Sign-Off
- [ ] Deployment procedure documented
- [ ] Rollback procedure tested
- [ ] Monitoring configured
- [ ] Alerts configured
- [ ] On-call procedures updated
- [ ] Approved for production

### Final Production Deployment
- [ ] All sign-offs obtained
- [ ] Deployment window scheduled
- [ ] Stakeholders notified
- [ ] Backup taken
- [ ] Deployment executed
- [ ] Post-deployment verification complete
- [ ] Monitoring active
- [ ] Success confirmed

---

## 📞 Support & Troubleshooting

### Issue: Build Fails
**Solution**:
1. `dotnet clean`
2. `dotnet restore`
3. `dotnet build`
4. Check error messages
5. Verify file locations
6. Rebuild solution

### Issue: Tests Fail
**Solution**:
1. Check test setup/teardown
2. Verify test database exists
3. Check connection string
4. Run migrations in test database
5. Check test data setup
6. Review test logs

### Issue: Endpoint Returns 500
**Solution**:
1. Check application logs
2. Check database logs
3. Verify DI configuration
4. Check request format
5. Add logging statements
6. Run in debug mode

### Issue: Timeout Errors
**Solution**:
1. Increase timeout value
2. Optimize queries (add indexes)
3. Check database performance
4. Monitor connection pool
5. Check for deadlocks
6. Review transaction scope

---

## 🎉 Deployment Successful!

Once all checkboxes are complete:
- ✅ Code is production-ready
- ✅ Testing comprehensive
- ✅ Documentation complete
- ✅ Monitoring active
- ✅ Team trained
- ✅ Ready to scale

**Status**: Ready for Production ✅
