# Observability — Strukturované Logování (Serilog)

## Setup & Konfigurace

```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Enrichers.Environment
dotnet add package Serilog.Enrichers.Thread
dotnet add package Serilog.Formatting.Compact
```

```csharp
// Program.cs — Serilog jako první věc před vším ostatním
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();  // zachytí chyby při startu

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, config) =>
        config
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProperty("Application", "MyApp")
            .WriteTo.Console(
                context.HostingEnvironment.IsDevelopment()
                    ? new ConsoleTheme()         // čitelný výstup pro dev
                    : new CompactJsonFormatter() // JSON pro produkci
            )
            .WriteTo.File(
                new CompactJsonFormatter(),
                path: "logs/app-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                fileSizeLimitBytes: 100 * 1024 * 1024));  // 100 MB

    // ... zbytek setup
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} ({Elapsed:0.0000} ms)";
        options.EnrichDiagnosticContext = (context, httpContext) =>
        {
            context.Set("UserId",     httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            context.Set("RemoteIP",   httpContext.Connection.RemoteIpAddress?.ToString());
            context.Set("UserAgent",  httpContext.Request.Headers.UserAgent.ToString());
        };
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed");
}
finally
{
    Log.CloseAndFlush();
}
```

```json
// appsettings.json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft":                                "Warning",
        "Microsoft.EntityFrameworkCore":            "Warning",
        "Microsoft.EntityFrameworkCore.Database.Command": "Information",
        "System.Net.Http.HttpClient":               "Warning"
      }
    }
  }
}
```

---

## Pravidla strukturovaného logování

```csharp
// ✓ VŽDY placeholder, NIKDY string interpolace
_logger.LogInformation("Order {OrderId} created by {UserId}", order.Id, userId);
// → Uloží jako strukturovaná data: { OrderId: 42, UserId: "abc" }

// ✗ String interpolace — ztráta struktury a zbytečná alokace
_logger.LogInformation($"Order {order.Id} created by {userId}");
// → Uloží jen jako string: "Order 42 created by abc"

// ✓ Složité objekty — destrukturuj s @, ale jen malé DTO bez PII
_logger.LogInformation("Processing {@OrderSummary}", new { order.Id, order.Status, LineCount = order.Lines.Count });
// → Serializuje objekt, ne jen ToString()
// ✗ {@Order} / {@Command} s celou entitou nebo commandem — PII, velikost logu, lazy-loading navigací

// ✓ Správné úrovně
_logger.LogTrace("Cache lookup for key {Key}: {Hit}", key, hit ? "HIT" : "MISS");
_logger.LogDebug("SQL query executed in {ElapsedMs}ms: {Query}", ms, sql);
_logger.LogInformation("User {UserId} logged in from {IP}", userId, ip);
_logger.LogWarning("Payment attempt {N}/3 failed for order {OrderId}", n, orderId);
_logger.LogError(ex, "Payment processing failed for order {OrderId}", orderId);
_logger.LogCritical(ex, "Database connection pool exhausted");
```

## Co logovat a co ne

```csharp
// ✓ Loguj
// - Business události (objednávka vytvořena, platba přijata)
// - Bezpečnostní události (přihlášení, odhlášení, neúspěšný pokus)
// - Výkonnostní anomálie (pomalé dotazy, timeout)
// - Integrační chyby (external API selhala)
// - Začátek/konec long-running operací

// ✗ Nikdy neloguj
_logger.LogInformation("Password: {Password}", password);     // NIKDY hesla
_logger.LogInformation("Token: {Token}", jwtToken);           // NIKDY tokeny
_logger.LogInformation("Card: {CardNumber}", cardNumber);     // NIKDY platební data
_logger.LogInformation("SSN: {SSN}", socialSecurityNumber);   // NIKDY osobní ID
_logger.LogDebug("Request body: {Body}", requestBody);        // POZOR — může obsahovat PII
```

## LoggerMessage.Define — pro hot paths

```csharp
// Eliminuje alokace v kritických cestách
public partial class OrderService
{
    // Source-generated logging (C# 10+ / .NET 6+)
    [LoggerMessage(Level = LogLevel.Information, Message = "Order {OrderId} created")]
    private partial void LogOrderCreated(int orderId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Payment retry {Attempt}/3 for order {OrderId}")]
    private partial void LogPaymentRetry(int attempt, int orderId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Payment failed for order {OrderId}")]
    private partial void LogPaymentFailed(Exception ex, int orderId);
}

// Použití
LogOrderCreated(order.Id);
LogPaymentRetry(attempt, orderId);
LogPaymentFailed(ex, orderId);
```

## Log Scopes — korelace skupiny operací

```csharp
using (_logger.BeginScope(new Dictionary<string, object>
{
    ["CorrelationId"] = correlationId,
    ["OrderId"]       = orderId,
    ["Operation"]     = "OrderProcessing"
}))
{
    _logger.LogInformation("Starting order processing");
    await ValidateAsync(ct);
    await ReserveStockAsync(ct);
    await ProcessPaymentAsync(ct);
    _logger.LogInformation("Order processing completed");
    // Všechny logy v tomto bloku mají CorrelationId, OrderId, Operation
}
```
