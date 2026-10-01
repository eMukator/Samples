# DDD — Taktické vzory

## Strongly-typed ID

```csharp
// ✓ Nelze omylem předat CustomerId tam, kde se čeká OrderId
public readonly record struct OrderId(Guid Value)
{
    public static OrderId New() => new(Guid.CreateVersion7());   // .NET 9+, časově řaditelné
    public override string ToString() => Value.ToString();
}

public readonly record struct CustomerId(Guid Value);

// ✗ Špatně
public Task ShipAsync(Guid orderId, Guid customerId);   // prohození se nezachytí
```

---

## Value Object

Nemá identitu, je immutable, porovnává se hodnotou, **validuje se při vzniku**.

```csharp
public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0) throw new DomainException("Částka nesmí být záporná.");
        if (currency is not { Length: 3 }) throw new DomainException("Měna musí být ISO 4217 kód.");
        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public static Money Zero(string currency) => new(0, currency);

    public Money Add(Money other)
    {
        if (other.Currency != Currency) throw new DomainException("Nelze sčítat různé měny.");
        return new(Amount + other.Amount, Currency);
    }
}

public sealed record Email
{
    public string Value { get; }
    public Email(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (!value.Contains('@')) throw new DomainException($"Neplatný e-mail: {value}");
        Value = value.Trim().ToLowerInvariant();
    }
}
```

- `record` dává hodnotovou rovnost zdarma — žádná vlastní base třída `ValueObject`.
- ✗ Nepoužívej `with` výraz na value objectu s validací v konstruktoru — `with` konstruktor obchází. Proto property bez `init`.

---

## Entity a Aggregate Root

```csharp
// Negenerický interface — kvůli ChangeTracker.Entries<IHasDomainEvents>() (viz ddd-persistence.md)
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}

public abstract class AggregateRoot<TId> : IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public TId Id { get; protected init; } = default!;
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

    protected void Raise(IDomainEvent @event) => _domainEvents.Add(@event);
    public void ClearDomainEvents() => _domainEvents.Clear();
}

public sealed class Order : AggregateRoot<OrderId>
{
    private readonly List<OrderLine> _lines = [];

    public CustomerId CustomerId { get; private set; }         // odkaz přes ID, ne navigace
    public OrderStatus Status { get; private set; }
    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();
    public Money Total => _lines.Aggregate(Money.Zero("CZK"), (sum, l) => sum.Add(l.Subtotal));

    private Order() { }   // pro EF Core

    public static Order Create(CustomerId customerId)
    {
        var order = new Order { Id = OrderId.New(), CustomerId = customerId, Status = OrderStatus.Draft };
        order.Raise(new OrderCreated(order.Id, customerId));
        return order;
    }

    public void AddLine(ProductId productId, int quantity, Money unitPrice)
    {
        EnsureStatus(OrderStatus.Draft);
        if (quantity <= 0) throw new DomainException("Množství musí být kladné.");

        var existing = _lines.FirstOrDefault(l => l.ProductId == productId);
        if (existing is not null) existing.IncreaseQuantity(quantity);
        else _lines.Add(new OrderLine(productId, quantity, unitPrice));
    }

    public void Confirm()
    {
        EnsureStatus(OrderStatus.Draft);
        if (_lines.Count == 0) throw new DomainException("Prázdnou objednávku nelze potvrdit.");
        Status = OrderStatus.Confirmed;
        Raise(new OrderConfirmed(Id, Total));
    }

    private void EnsureStatus(OrderStatus expected)
    {
        if (Status != expected)
            throw new DomainException($"Operace vyžaduje stav {expected}, objednávka je {Status}.");
    }
}

// Vnitřní entita — mění se jen přes root, proto internal metody
public sealed class OrderLine
{
    public ProductId ProductId { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; } = default!;
    public Money Subtotal => new(UnitPrice.Amount * Quantity, UnitPrice.Currency);

    private OrderLine() { }
    internal OrderLine(ProductId productId, int quantity, Money unitPrice)
        => (ProductId, Quantity, UnitPrice) = (productId, quantity, unitPrice);

    internal void IncreaseQuantity(int by) => Quantity += by;
}
```

```csharp
// ✗ Anemický model — logika uniká do služeb, invarianty nikdo nehlídá
public class Order
{
    public Guid Id { get; set; }
    public string Status { get; set; }
    public List<OrderLine> Lines { get; set; }
}
orderService.Confirm(order);          // pravidla roztroušená po službách
order.Lines.Add(new OrderLine());     // obejde všechna pravidla
```

---

## Pravidla pro návrh agregátu

1. **Malé agregáty.** Agregát = hranice konzistence, ne „všechno, co spolu souvisí". Customer a jeho Orders jsou dva agregáty.
2. **Jedna transakce mění jeden agregát.** Potřeba změnit dva → domain event + eventual consistency (viz `event-driven/`).
3. **Odkaz přes ID.** `Order.CustomerId`, nikdy `Order.Customer`.
4. **Celý agregát se načítá a ukládá najednou.** Žádné částečné načtení přes `Include` podle potřeby.

---

## Výjimky vs. Result v doméně

| Situace | Použij |
|---------|--------|
| Porušení invariantu (programátorská chyba / nevalidní vstup, který měl zachytit validator) | `DomainException` |
| Očekávaný business výsledek („nedostatek zboží", „limit překročen") | `Result` z handleru (viz `cqrs/`) |

```csharp
public sealed class DomainException(string message) : Exception(message);
// Middleware mapuje DomainException → 422 ProblemDetails (viz api-design/)
```

---

## Domain Service

Logika, která nepatří jedné entitě, typicky potřebuje více agregátů **ke čtení**.

```csharp
// ✓ Čistá doménová logika bez I/O — žádný interface, testuj přímo
public sealed class PricingService
{
    public Money CalculateDiscount(Order order, CustomerTier tier) => tier switch
    {
        CustomerTier.Gold => new(order.Total.Amount * 0.1m, order.Total.Currency),
        _ => Money.Zero(order.Total.Currency)
    };
}
```

✗ Domain service nevolá DB, HTTP ani `SaveChanges` — to dělá application handler.

---

## Domain Events

```csharp
public interface IDomainEvent;   // marker; v Domain projektu bez závislostí

public sealed record OrderConfirmed(OrderId OrderId, Money Total) : IDomainEvent;
```

- Pojmenování v **minulém čase** — popisují, co se stalo.
- Obsahují jen data potřebná pro reakci (ID + klíčové hodnoty), ne celý agregát.
- Dispatch viz `ddd-persistence.md`; integrace mimo bounded context viz `event-driven/`.
