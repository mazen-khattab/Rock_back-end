# 🎯 Mapper Logging - Quick Reference Card

## ONE-LINE SETUP

```csharp
Mapper.SetLogger(app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Mapper"));
```

---

## INITIALIZATION EXAMPLES

### Program.cs (ASP.NET Core 6+)
```csharp
var app = builder.Build();
Mapper.SetLogger(app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Mapper"));
app.Run();
```

### Startup.cs
```csharp
public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
{
    Mapper.SetLogger(loggerFactory.CreateLogger("Mapper"));
}
```

---

## WHAT GETS LOGGED

| Method | Entry | Exit | Warnings | Errors | Debug |
|--------|:-----:|:----:|:--------:|:------:|:-----:|
| MapToDtoList<Product> | ✓ | ✓ | - | ✓ | ✓ |
| MapToDtoList<Variant> | ✓ | ✓ | - | ✓ | ✓ |
| MapToDtoList<VariantImage> | ✓ | ✓ | - | ✓ | ✓ |
| MapToDtoList<Cart> | ✓ | ✓ | - | ✓ | ✓ |
| MapToDto<Product> | ✓ | ✓ | ✓ | ✓ | - |
| MapToDto<Variant> | ✓ | ✓ | ✓ | ✓ | - |
| MapToDto<VariantImage> | ✓ | ✓ | ✓ | ✓ | - |
| MapToDto<Cart> | ✓ | ✓ | ✓ | ✓ | - |

---

## TYPICAL LOG OUTPUT

```
info: Mapper MapToDtoList<Product>: Starting mapping for 5 products with languageId: 1
dbug: Mapper MapToDtoList<Product>: Successfully mapped product with Id: 1
dbug: Mapper MapToDtoList<Product>: Successfully mapped product with Id: 2
dbug: Mapper MapToDtoList<Product>: Successfully mapped product with Id: 3
warn: Mapper MapToDto<Product>: No translation found for product 4 with languageId: 1
dbug: Mapper MapToDtoList<Product>: Successfully mapped product with Id: 4
dbug: Mapper MapToDtoList<Product>: Successfully mapped product with Id: 5
info: Mapper MapToDtoList<Product>: Completed mapping 5 products
```

---

## LOGGING LEVELS

| Level | When Used | Examples |
|-------|-----------|----------|
| **Information** | Normal flow, method entry/exit | "Starting mapping...", "Completed mapping..." |
| **Warning** | Potential issues, missing data | "No translation found...", "Null property..." |
| **Error** | Failures and exceptions | "Parameter is null", "Error mapping..." |
| **Debug** | Detailed tracking | Individual item mappings |

---

## CONFIGURATION

### Console Only
```csharp
builder.Services.AddLogging(config => config.AddConsole());
```

### Console + File
```csharp
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddFile("logs/mapper-{Date}.txt");
});
```

### Only Warnings & Errors
```csharp
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Warning);
});
```

---

## DISABLE LOGGING (keep code, just don't initialize)

```csharp
// Don't call Mapper.SetLogger() 
// Logging will be null-safe and skipped
```

---

## TROUBLESHOOTING

**No logs showing?**
→ Call `Mapper.SetLogger()` during app startup

**Logs too verbose?**
→ Set minimum log level to Warning

**Want logs in file?**
→ Use Serilog: `config.AddFile("logs/mapper-{Date}.txt")`

**Performance concerns?**
→ Set level to Information (skip Debug)

---

## FILES TO READ

| File | Purpose | Read Time |
|------|---------|-----------|
| `MAPPER_LOGGING_SETUP.md` | Detailed setup guide | 10 min |
| `MAPPER_LOGGING_SUMMARY.md` | Complete reference | 15 min |
| `MAPPER_LOGGING_VISUAL.txt` | Visual guide | 10 min |

---

## KEY METHODS

```csharp
// Initialize logging
Mapper.SetLogger(ILogger logger);

// All mapping methods now log automatically
Mapper.MapToDtoList(products, languageId);
Mapper.MapToDto(product, languageId);
Mapper.MapToDto(variant, languageId);
Mapper.MapToDto(image);
Mapper.MapToDto(cart);
```

---

## BACKWARD COMPATIBLE ✅

- ✓ Existing code works without changes
- ✓ Logging is optional
- ✓ No performance cost if logger not set
- ✓ Safe null checks everywhere

---

**Status**: ✅ Ready to Use
