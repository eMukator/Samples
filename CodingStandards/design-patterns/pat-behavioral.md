# Behaviorální vzory

## Strategy

Zaměnitelný algoritmus vybraný za běhu. Varianta s keyed services viz `design-principles/dp-solid.md` (OCP).

```csharp
// ✓ Varianta „první, kdo umí" — když výběr závisí na víc než jednom klíči
public interface IShippingRule
{
    bool CanHandle(Order order);
    Money Calculate(Order order);
}

public sealed class ShippingCalculator(IEnumerable<IShippingRule> rules)
{
    public Money Calculate(Order order)
        => (rules.FirstOrDefault(r => r.CanHandle(order))
            ?? throw new DomainException($"Žádné pravidlo dopravy pro objednávku {order.Id}."))
           .Calculate(order);
}

// ✓ Jednoduchá strategie = delegát, žádný interface
public sealed class RetryPolicy(Func<int, TimeSpan> delay) { ... }
```

**Kdy ne:** dvě větve, které se nemění → `if`.

---

## Specification

Pojmenované, znovupoužitelné doménové kritérium, přeložitelné do SQL.

```csharp
public static class OrderSpecs
{
    public static Expression<Func<Order, bool>> Overdue(DateTimeOffset now)
        => o => o.Status == OrderStatus.Confirmed && o.DueDate < now;

    public static Expression<Func<Order, bool>> ForCustomer(CustomerId id)
        => o => o.CustomerId == id;
}

var overdue = await db.Orders.Where(OrderSpecs.Overdue(clock.GetUtcNow())).ToListAsync(ct);
```

- Statické metody vracející `Expression` stačí — ✗ framework `ISpecification<T>` s `And`/`Or`/`Not` třídami, dokud ho nepotřebuješ.
- **Kdy ne:** kritérium použité na jednom místě → piš `Where` přímo.

---

## State

Chování závisí na stavu a přechody mají pravidla.

```csharp
// ✓ Většinou stačí enum + doménové metody s guardy (viz ddd/ddd-tactical.md — Order.Confirm())

// ✓ Explicitní tabulka přechodů, když stavů je hodně
private static readonly FrozenDictionary<(OrderStatus From, OrderTrigger Trigger), OrderStatus> Transitions =
    new Dictionary<(OrderStatus, OrderTrigger), OrderStatus>
    {
        [(OrderStatus.Draft, OrderTrigger.Confirm)]     = OrderStatus.Confirmed,
        [(OrderStatus.Confirmed, OrderTrigger.Ship)]    = OrderStatus.Shipped,
        [(OrderStatus.Confirmed, OrderTrigger.Cancel)]  = OrderStatus.Cancelled,
        [(OrderStatus.Shipped, OrderTrigger.Deliver)]   = OrderStatus.Delivered,
    }.ToFrozenDictionary();

private void Fire(OrderTrigger trigger)
{
    if (!Transitions.TryGetValue((Status, trigger), out var next))
        throw new DomainException($"Přechod {trigger} není ze stavu {Status} povolen.");
    Status = next;
}
```

- Knihovna `Stateless` — jen pro složité automaty s guardy, vstupními/výstupními akcemi a hierarchií stavů.
- ✗ Třída na každý stav (klasický GoF State) — v C# zřídka lepší než tabulka.

---

## Chain of Responsibility → middleware / pipeline

```csharp
// ✓ ASP.NET Core middleware JE chain of responsibility
app.Use(async (ctx, next) =>
{
    if (!ctx.Request.Headers.ContainsKey("X-Tenant")) { ctx.Response.StatusCode = 400; return; }
    await next(ctx);
});

// ✓ Handler pipeline přes decoratory (cqrs/cqrs-pipeline.md)
// ✓ DelegatingHandler pro HttpClient
internal sealed class ApiKeyHandler(IOptions<PartnerOptions> o) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        request.Headers.Add("X-Api-Key", o.Value.ApiKey);
        return base.SendAsync(request, ct);
    }
}
```

✗ Vlastní `IHandler.SetNext()` řetězy — framework má lepší mechanismus.

---

## Observer → události

| Potřeba | Mechanismus |
|---------|-------------|
| Reakce uvnitř transakce agregátu | Domain events (`ddd/`, `event-driven/`) |
| Reakce mimo transakci, spolehlivě | Integration events přes Outbox |
| In-process notifikace UI (Blazor) | C# `event` + `InvokeAsync` (`blazor/blazor-data-state.md`) |
| Fronta v procesu | `Channel<T>` |
| Streamy událostí s operátory (throttle, buffer) | `IObservable<T>` + System.Reactive |

⚠ C# `event`: odběratel se musí odhlásit (memory leak), výjimka v handleru přeruší ostatní odběratele.

---

## Command

Operace jako objekt (fronta, undo, audit, retry).

```csharp
// ✓ V CQRS je command record + handler (cqrs/cqrs-basics.md)
public sealed record ConfirmOrderCommand(OrderId OrderId);
```

Undo/redo v UI → zásobník commandů s `Execute` / `Undo` — jen když je undo požadavek.

---

## Template Method → kompozice

```csharp
// ✗ Abstraktní základ s „háčky" — křehká dědičnost
public abstract class ImportBase { public void Run() { var d = Read(); Validate(d); Save(d); } protected abstract … }

// ✓ Kompozice — kroky jako závislosti
public sealed class Importer<T>(IReader<T> reader, IValidator<T> validator, IWriter<T> writer) { ... }
```

Výjimka: frameworkové základní třídy (`BackgroundService.ExecuteAsync`, `ComponentBase`) — tam je template method v pořádku.

---

## Mediator

Viz `cqrs/cqrs-basics.md` — výchozí je přímá injekce handleru, mediator knihovna jen vědomým rozhodnutím.
