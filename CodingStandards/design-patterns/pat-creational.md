# Vytvářecí vzory

## Singleton → DI

```csharp
// ✗ Klasický singleton — globální stav, netestovatelné, skrytá závislost
public sealed class ExchangeRateCache
{
    public static ExchangeRateCache Instance { get; } = new();
    private ExchangeRateCache() { }
}

// ✓ Životnost řídí kontejner, závislost je vidět v konstruktoru
builder.Services.AddSingleton<ExchangeRateCache>();
```

Singleton musí být **thread-safe** a nesmí držet scoped závislosti (captive dependency, viz `csharp/architecture.md`).

---

## Factory

**Kdy ano:** o konkrétním typu nebo parametrech objektu se rozhoduje za běhu, nebo objekt potřebuje runtime data + služby z DI.

```csharp
// ✓ Func<> factory z DI — bez vlastní factory třídy
builder.Services.AddTransient<ReportGenerator>();
builder.Services.AddSingleton<Func<ReportGenerator>>(sp => () => sp.GetRequiredService<ReportGenerator>());

// ✓ ActivatorUtilities — runtime parametr + služby z DI
public sealed class ImportJobFactory(IServiceProvider sp)
{
    public ImportJob Create(ImportFile file) => ActivatorUtilities.CreateInstance<ImportJob>(sp, file);
}

// ✓ Keyed services (.NET 8+) — výběr implementace podle klíče
builder.Services.AddKeyedScoped<IDocumentExporter, PdfExporter>("pdf");
builder.Services.AddKeyedScoped<IDocumentExporter, XlsxExporter>("xlsx");

public sealed class ExportHandler([FromKeyedServices("pdf")] IDocumentExporter pdf) { ... }
// nebo za běhu: sp.GetRequiredKeyedService<IDocumentExporter>(format)
```

**Static factory metoda** — doménové objekty (viz `ddd/ddd-tactical.md`):

```csharp
public static Order Create(CustomerId customerId) { ... }   // pojmenovaný konstruktor s validací a eventem
public static Result<Email> TryCreate(string value) { ... } // varianta bez výjimky
```

**Kdy ne:**
- ✗ `IOrderServiceFactory`, která jen volá `new OrderService(...)` — to dělá DI.
- ✗ Abstract Factory pro jednu rodinu objektů.

---

## Builder

**Kdy ano:** objekt s mnoha volitelnými parametry, postupné skládání, **testovací data**.

```csharp
// ✓ Testovací data (viz testing-advanced/test-builders-e2e.md)
var order = new OrderBuilder()
    .ForCustomer(customerId)
    .WithLine(productId, quantity: 2, price: 100m)
    .Confirmed()
    .Build();

// ✓ Frameworkové buildery — používej, nepiš vlastní
var app = WebApplication.CreateBuilder(args);
var pipeline = new ResiliencePipelineBuilder().AddRetry(...).Build();
```

**Kdy ne:**
- ✗ Builder pro produkční DTO / record — použij `required` properties a object initializer.

```csharp
// ✓ Místo builderu
public sealed record CreateOrderRequest
{
    public required Guid CustomerId { get; init; }
    public required IReadOnlyList<OrderLineRequest> Lines { get; init; }
    public string? Note { get; init; }
}
```

---

## Options pattern (konfigurace)

```csharp
public sealed class SmtpOptions
{
    public const string Section = "Smtp";

    [Required] public string Host { get; init; } = "";
    [Range(1, 65535)] public int Port { get; init; } = 587;
}

builder.Services.AddOptions<SmtpOptions>()
    .BindConfiguration(SmtpOptions.Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Injektuj IOptions<T> (singleton hodnoty), IOptionsSnapshot<T> (per request), IOptionsMonitor<T> (změny za běhu)
```

Podrobnosti viz `environment-config/env-appsettings.md`.

---

## Object Pool

**Kdy ano:** drahé objekty vytvářené velmi často (měřeno!) — `StringBuilder` ve vysokém throughputu, parsery.

```csharp
builder.Services.AddSingleton(ObjectPool.Create(new StringBuilderPooledObjectPolicy()));
```

**Kdy ne:** běžné objekty — GC je pro krátce žijící objekty velmi efektivní. Pro pole použij `ArrayPool` (viz `performance/`).
