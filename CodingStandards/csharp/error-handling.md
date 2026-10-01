# C# — Error Handling & Logging

## Výjimky — základní pravidla

- Výjimky jsou pro **neočekávané stavy**, ne pro řízení toku (flow control)
- Vždy chytej specifické typy — nikdy holé `catch (Exception)` bez důvodu
- Vždy loguj výjimky před znovu-vyhozením nebo potlačením
- Nikdy nevolej `throw ex` (maže stack trace) — vždy jen `throw`
- Custom výjimky dědí z `Exception`, mají suffix `Exception`

### Výjimka vs. Result

| Situace | Mechanismus |
|---------|-------------|
| Očekávaný business výsledek (nenalezeno, nedostatek zboží, konflikt stavu) | `Result` s typovaným `Error` (viz `cqrs/cqrs-basics.md`) |
| Porušení doménového invariantu | `DomainException` (viz `ddd/ddd-tactical.md`) → 422 |
| Nevalidní vstup | `ValidationException` z validátoru → 400 |
| Infrastrukturní chyba (DB, síť), programátorská chyba | Výjimka → globální handler → 500 |

Custom výjimky níže (`OrderNotFoundException`, `InsufficientStockException`) jsou pro projekty bez Result patternu. V projektech s CQRS vracej tyto stavy jako `Result`.

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

Použij `IExceptionHandler` s odpovědí ve formátu RFC 9457 ProblemDetails — kompletní implementace viz `api-design/api-versioning-errors-pagination.md` (`GlobalExceptionHandler`).

✗ Vlastní JSON formát chyb (`new { error = message }`) — klienti pak musí zpracovávat dva formáty.

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

// ✓ [LoggerMessage] source generator pro výkonnostně kritické cesty (eliminuje alokace)
//   — nahrazuje ruční LoggerMessage.Define; viz observability/obs-logging.md
[LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Order {OrderId} created successfully")]
private partial void LogOrderCreated(int orderId);

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
