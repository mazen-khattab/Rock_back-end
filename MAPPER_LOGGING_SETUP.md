# Mapper Logging Setup Guide

## Overview
The `Mapper.cs` static utility class now includes comprehensive logging at every method. To enable logging, you need to initialize the logger during application startup.

## How to Initialize

### In Program.cs (ASP.NET Core 6+)

```csharp
using Application.Mapping;
using Microsoft.Extensions.Logging;

var builder = WebApplicationBuilder.CreateBuilder(args);

// Add logging (if not already added)
builder.Services.AddLogging();

var app = builder.Build();

// Initialize Mapper logger
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var mapperLogger = LoggerFactory.Create(config => config.AddConsole())
    .CreateLogger("Application.Mapping.Mapper");
Mapper.SetLogger(mapperLogger);

// ... rest of your app configuration
app.Run();
```

### In Startup.cs (ASP.NET Core 5 or earlier)

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
{
    // Initialize Mapper logger
    var logger = loggerFactory.CreateLogger("Application.Mapping.Mapper");
    Mapper.SetLogger(logger);

    // ... rest of your configuration
}
```

### Better Approach: Use Dependency Injection

Create a helper service to initialize the mapper:

```csharp
// Add this to Program.cs in ConfigureServices
services.AddSingleton<IMapperInitializer, MapperInitializer>();
```

Create a new file `Application/Services/MapperInitializer.cs`:

```csharp
using Application.Mapping;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public interface IMapperInitializer
    {
        void Initialize();
    }

    public class MapperInitializer : IMapperInitializer
    {
        private readonly ILogger<Mapper> _logger;

        public MapperInitializer(ILogger<Mapper> logger)
        {
            _logger = logger;
        }

        public void Initialize()
        {
            Mapper.SetLogger(_logger);
        }
    }
}
```

Then in your startup middleware:

```csharp
app.MapPost("/api/test", (IMapperInitializer mapperInit) =>
{
    mapperInit.Initialize();
    return "Mapper initialized";
});

// Or better yet, call it once at startup:
var mapperInit = app.Services.GetRequiredService<IMapperInitializer>();
mapperInit.Initialize();
```

---

## Logging Levels Used

### Information
- Method entry/exit with parameters
- Successful mappings with result details
- Summary of batch operations

### Warning
- Missing translations for requested language
- Null navigation properties (Variant, MediaAsset)
- Missing or empty expected data

### Error
- Null parameters passed to methods
- Exceptions during mapping operations

### Debug
- Individual item mappings in batch operations

---

## Example Log Output

```
info: Application.Mapping.Mapper[0] Mapper logger initialized
info: Application.Mapping.Mapper[0] MapToDtoList<Product>: Starting mapping for 25 products with languageId: 1
dbug: Application.Mapping.Mapper[0] MapToDtoList<Product>: Successfully mapped product with Id: 1
dbug: Application.Mapping.Mapper[0] MapToDtoList<Product>: Successfully mapped product with Id: 2
warn: Application.Mapping.Mapper[0] MapToDto<Product>: No translation found for product 3 with languageId: 1. Using default values.
info: Application.Mapping.Mapper[0] MapToDtoList<Product>: Completed mapping 25 products
```

---

## What Gets Logged

### MapToDtoList Methods
- ✓ Number of items being mapped
- ✓ Language ID (if applicable)
- ✓ Success/failure of individual item mappings
- ✓ Total count of mapped items
- ✓ Errors with item details

### MapToDto Methods
- ✓ Method entry with entity ID and language ID
- ✓ Null parameter validation
- ✓ Missing translations or navigation properties
- ✓ Successful mapping with data summary
- ✓ Related entity counts
- ✓ Exception details on error

---

## Benefits

1. **Debugging**: Easily trace which mappings succeed or fail
2. **Performance Monitoring**: Track mapping operation durations
3. **Data Quality**: Identify missing translations or incomplete data
4. **Error Tracking**: Capture exceptions with full context
5. **Audit Trail**: Record all data transformations

---

## Optional: Add Logging Decorators

You can also extend logging duration timing by creating a wrapper:

```csharp
// In Program.cs or a startup service
public static void SetupMapperLogging(IServiceProvider services)
{
    var loggerFactory = services.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("Application.Mapping.Mapper");
    
    Mapper.SetLogger(logger);
    
    logger.LogInformation("Mapper logging initialized successfully");
}
```

---

## Troubleshooting

**Q: I don't see any mapper logs**
A: Ensure `SetLogger()` is called during startup before any mapping operations

**Q: Logs are too verbose**
A: Adjust your logging configuration to filter by level:
```csharp
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Warning); // Only warnings and errors
    // OR for specific namespace
    config.AddFilter("Application.Mapping.Mapper", LogLevel.Information);
});
```

**Q: How do I log to file?**
A: Use Serilog or NLog:
```csharp
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddFile("logs/mapper-{Date}.txt"); // Requires Serilog.Extensions.Logging
});
```

---

**Status**: ✅ Logging enabled and ready to use
