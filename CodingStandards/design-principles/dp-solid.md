# SOLID v moderním C#

## S — Single Responsibility

Třída má **jeden důvod ke změně** (jednoho „aktéra", který o ní rozhoduje).

```csharp
// ✗ Špatně — tři důvody ke změně: business pravidla, formát exportu, doručení
public class InvoiceManager
{
    public decimal CalculateTotal(Invoice invoice) { ... }
    public byte[] ExportToPdf(Invoice invoice) { ... }
    public Task SendByEmailAsync(Invoice invoice) { ... }
}

// ✓ Správně — každá třída se mění z jednoho důvodu
public sealed class InvoiceCalculator { public decimal CalculateTotal(Invoice invoice) { ... } }
public sealed class InvoicePdfExporter { public byte[] Export(Invoice invoice) { ... } }
public sealed class InvoiceMailer(IEmailService email) { public Task SendAsync(Invoice invoice, CancellationToken ct) { ... } }
```

Varovné signály: název obsahuje `Manager`, `Helper`, `Utils`, `Processor`; konstruktor má > 5 závislostí; soubor > 300 řádků.

---

## O — Open/Closed

Nové chování přidávej **novým kódem**, ne úpravou existujícího `switch`.

```csharp
// ✗ Špatně — každý nový typ dopravy = úprava této metody (a všech podobných)
public decimal CalculateShipping(Order order) => order.ShippingType switch
{
    ShippingType.Post    => 99m,
    ShippingType.Courier => 149m + order.Weight * 10,
    ShippingType.Pickup  => 0m,
    _ => throw new ArgumentOutOfRangeException()
};

// ✓ Správně — strategie registrované v DI (keyed services, .NET 8+)
public interface IShippingCalculator
{
    decimal Calculate(Order order);
}

public sealed class CourierShipping : IShippingCalculator
{
    public decimal Calculate(Order order) => 149m + order.Weight * 10;
}

builder.Services.AddKeyedSingleton<IShippingCalculator, PostShipping>(ShippingType.Post);
builder.Services.AddKeyedSingleton<IShippingCalculator, CourierShipping>(ShippingType.Courier);

// Použití
public sealed class ShippingService(IServiceProvider sp)
{
    public decimal Calculate(Order order)
        => sp.GetRequiredKeyedService<IShippingCalculator>(order.ShippingType).Calculate(order);
}
```

> Jeden `switch` na jednom místě je v pořádku. Problém je **stejný switch opakovaný** na více místech — tehdy přejdi na polymorfismus.

---

## L — Liskov Substitution

Potomek musí jít použít všude, kde rodič, **bez překvapení**.

```csharp
// ✗ Špatně — ReadOnlyRepository porušuje kontrakt rodiče
public class ReadOnlyOrderRepository : OrderRepository
{
    public override Task AddAsync(Order order, CancellationToken ct)
        => throw new NotSupportedException();   // volající to nečeká
}

// ✓ Správně — rozděl kontrakt (viz ISP)
public interface IOrderReader { Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct); }
public interface IOrderWriter { Task AddAsync(Order order, CancellationToken ct); }
```

Varovné signály: `NotSupportedException` / `NotImplementedException` v override, kontrola `if (x is SpecificSubtype)`, potomek zpřísňuje vstupní podmínky.

---

## I — Interface Segregation

Interface navrhuj **z pohledu konzumenta**, ne implementace.

```csharp
// ✗ Špatně — reportovací služba závisí na 12 metodách, používá jednu
public interface IOrderService
{
    Task<OrderDto> CreateAsync(...);
    Task CancelAsync(...);
    Task<decimal> GetMonthlyRevenueAsync(...);
    // ... dalších 9 metod
}

// ✓ Správně — malé, účelové kontrakty
public interface IRevenueQuery
{
    Task<decimal> GetMonthlyRevenueAsync(YearMonth month, CancellationToken ct);
}
```

V CQRS (viz `cqrs/`) je ISP splněno přirozeně — jeden handler = jeden use case.

---

## D — Dependency Inversion

Vysokoúrovňová logika závisí na **abstrakci, kterou sama vlastní**. Infrastruktura ji implementuje.

```csharp
// ✗ Špatně — doména/aplikace přímo závisí na infrastruktuře
public sealed class OrderHandler
{
    private readonly SmtpClient _smtp = new("smtp.firma.cz");   // new na službě
    private readonly AppDbContext _db = new();                     // nelze testovat
}

// ✗ Špatně — service locator schovává závislosti
public sealed class OrderHandler(IServiceProvider sp)
{
    public Task Handle() => sp.GetRequiredService<IEmailService>().SendAsync(...);
}

// ✓ Správně — interface v Application, implementace v Infrastructure
public sealed class OrderHandler(IOrderRepository orders, IEmailService email, TimeProvider clock) { ... }
```

- Čas přes `TimeProvider` (.NET 8+), ne `DateTime.Now` — testovatelnost.
- `IServiceProvider` injektuj jen ve factory / dispatcher infrastruktuře, nikdy v business kódu.
