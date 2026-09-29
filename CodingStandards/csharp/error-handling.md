# C# — Error Handling & Logging

## Výjimky — základní pravidla

- Výjimky jsou pro **neočekávané stavy**, ne pro řízení toku (flow control)
- Vždy chytej specifické typy — nikdy holé `catch (Exception)` bez důvodu
- Vždy loguj výjimky před znovu-vyhozením nebo potlačením
- Nikdy nevolej `throw ex` (maže stack trace) — vždy jen `throw`
- Custom výjimky dědí z `Exception`, mají suffix `Exception`

```csharp
// ✓ Specifická výjimka
try
{
    await _repository.UpdateAsync(order, ct);
}
catch (DbUpdateConcurrencyException ex)
{
    _logger.LogWarning(ex, "Concurrency conflict for order {OrderId}", order.Id);
    throw new OrderUpdateConflictException(order.Id, ex);
}

// ✗ Polykání výjimky
try { await DoSomethingAsync(); }
catch (Exception) { }  // NIKDY

// ✗ Ztrácení stack trace
try { await DoSomethingAsync(); }
catch (Exception ex) { throw ex; }  // CHYBA — použij jen throw

// ✓ Správně
try { await DoSomethingAsync(); }
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error in DoSomethingAsync");
    throw;
}
```

## Custom výjimky

```csharp
// Definuj pro každý typ doménové chyby
public class OrderNotFoundException(int orderId)
    : Exception($"Objednávka #{orderId} nebyla nalezena")
{
    public int OrderId { get; } = orderId;
}

public class OrderUpdateConflictException(int orderId, Exception? inner = null)
    : Exception($"Konflikt při aktualizaci objednávky #{orderId}", inner)
{
    public int OrderId { get; } = orderId;
}

public class InsufficientStockException(string productSku, int requested, int available)
    : Exception($"Nedostatek skladu pro {productSku}: požadováno {requested}, dostupné {available}")
{
    public string ProductSku { get; } = productSku;
    public int Requested { get; } = requested;
    public int Available { get; } = available;
}
```

## Global exception handling v ASP.NET Core

```csharp
// Program.cs
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

        var (statusCode, message) = exception switch
        {
            OrderNotFoundException ex => (StatusCodes.Status404NotFound, ex.Message),
            ValidationException ex    => (StatusCodes.Status400BadRequest, ex.Message),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Přístup odepřen"),
            _                         => (StatusCodes.Status500InternalServerError, "Interní chyba serveru")
        };

        if (statusCode == 500)
            logger.LogError(exception, "Unhandled exception");

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = message });
    });
});
```

## Logging

### Úrovně logování

| Level | Kdy |
|-------|-----|
| `LogTrace` | Detailní debug info (vypnuto v produkci) |
| `LogDebug` | Debug info pro vývojáře |
| `LogInformation` | Normální průběh aplikace |
| `LogWarning` | Neočekávaný stav, ale aplikace funguje |
| `LogError` | Chyba — operace selhala |
| `LogCritical` | Fatální selhání — aplikace musí být zastavena |

### Strukturované logování

```csharp
// ✓ Strukturované logování — VŽDY použij placeholders, ne string interpolaci
_logger.LogInformation("Order {OrderId} created for customer {CustomerId}",
    order.Id, order.CustomerId);

// ✗ String interpolace v logu — ztráta struktury, zbytečná alokace
_logger.LogInformation($"Order {order.Id} created for customer {order.CustomerId}");

// ✓ LoggerMessage.Define pro výkonnostně kritické cesty (eliminuje alokace)
private static readonly Action<ILogger, int, Exception?> _orderCreated =
    LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(1001, "OrderCreated"),
        "Order {OrderId} created successfully");

_orderCreated(_logger, order.Id, null);

// ✓ Vždy loguj s kontextem — co se stalo, s jakými daty
_logger.LogError(ex, "Failed to process payment for order {OrderId}, amount {Amount:C}",
    order.Id, order.Total);

// ✗ Log bez kontextu
_logger.LogError("Payment failed");  // k ničemu
```

### Co logovat

```csharp
// ✓ Loguj začátek/konec důležitých operací
_logger.LogInformation("Starting order import, batch size: {BatchSize}", items.Count);
// ... zpracování ...
_logger.LogInformation("Order import completed: {Processed} processed, {Failed} failed",
    processed, failed);

// ✓ Loguj důležité business události
_logger.LogInformation("Order {OrderId} status changed from {OldStatus} to {NewStatus}",
    order.Id, oldStatus, newStatus);

// ✗ Nikdy neloguj citlivé údaje
_logger.LogDebug("User login: {Email}, password: {Password}", email, password);  // BEZPEČNOSTNÍ CHYBA
_logger.LogDebug("Payment token: {Token}", paymentToken);  // BEZPEČNOSTNÍ CHYBA
```

### Scopy pro korelaci

```csharp
// ✓ LogScope pro korelaci logů v rámci operace
using (_logger.BeginScope(new Dictionary<string, object>
{
    ["OrderId"] = order.Id,
    ["CustomerId"] = order.CustomerId,
    ["CorrelationId"] = correlationId
}))
{
    _logger.LogInformation("Processing order");
    await ValidateAsync(order, ct);
    await SaveAsync(order, ct);
    _logger.LogInformation("Order processed successfully");
}
```
