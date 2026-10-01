# Modulární monolit — komunikace mezi moduly

## Tři způsoby, v tomto pořadí preference

| Způsob | Kdy | Vazba |
|--------|-----|-------|
| **Integration event** (async, Outbox) | Reakce na změnu v jiném modulu | Nejvolnější |
| **Synchronní kontrakt** (in-process interface) | Potřebuji data hned pro rozhodnutí (validace, výpočet) | Střední |
| **Lokální kopie dat** (replikace událostmi) | Data z jiného modulu čtu často (seznamy, reporty) | Volná, ale eventual consistency |

✗ Nikdy: přímý přístup do cizího `DbContext`, cizích tabulek nebo `internal` typů přes reflexi.

---

## Synchronní kontrakt

```csharp
// MyApp.Catalog.Contracts — public, stabilní API modulu
public interface ICatalogApi
{
    Task<ProductPriceDto?> GetPriceAsync(Guid productId, CancellationToken ct);
    Task<IReadOnlyList<ProductSummaryDto>> GetSummariesAsync(IReadOnlyCollection<Guid> productIds, CancellationToken ct);
}

public sealed record ProductPriceDto(Guid ProductId, decimal Amount, string Currency, bool IsAvailable);

// MyApp.Catalog — internal implementace, čte přes vlastní DbContext
internal sealed class CatalogApi(CatalogDbContext db) : ICatalogApi
{
    public Task<ProductPriceDto?> GetPriceAsync(Guid productId, CancellationToken ct)
        => db.Products.AsNoTracking()
            .Where(p => p.Id == new ProductId(productId))
            .Select(p => new ProductPriceDto(p.Id.Value, p.Price.Amount, p.Price.Currency, p.IsAvailable))
            .FirstOrDefaultAsync(ct);
    // …
}

// MyApp.Ordering — použití
internal sealed class AddOrderLineHandler(IOrderRepository orders, ICatalogApi catalog, IUnitOfWork uow) { ... }
```

Pravidla kontraktu:
- DTO s primitivními typy — ne doménové typy jiného modulu.
- Dávkové metody (`GetSummariesAsync(ids)`), ne volání v cyklu (N+1 mezi moduly).
- Kontrakt **jen čte**. Změnu stavu v jiném modulu vyvolej událostí nebo commandem v jeho kontraktu, nikdy ne přes sdílenou transakci.
- Kontrakt je API — změny jen aditivně (jako u integration events).

---

## Integration events mezi moduly

```csharp
// MyApp.Ordering.Contracts
public sealed record OrderConfirmedIntegrationEvent(Guid OrderId, Guid CustomerId, decimal TotalAmount, string Currency) : IIntegrationEvent;

// MyApp.Billing — reaguje, vlastní transakce, idempotentně
internal sealed class CreateInvoiceOnOrderConfirmed(BillingDbContext db, …)
    : IIntegrationEventHandler<OrderConfirmedIntegrationEvent> { ... }
```

- Publikace přes Outbox modulu-odesílatele (viz `event-driven/ed-outbox-inbox.md`).
- In-process doručení stačí (Outbox publisher volá handlery ostatních modulů přímo, nebo přes in-memory transport Wolverine / MassTransit) — broker není nutný, dokud neoddělíš modul do služby.
- I in-process konzument musí být **idempotentní** — připravuje to na pozdější oddělení.

---

## Lokální kopie (read model)

```csharp
// Ordering potřebuje jméno zákazníka v seznamu objednávek — nečte z modulu Customers při každém dotazu
internal sealed class CustomerSnapshot        // ordering.CustomerSnapshots
{
    public Guid CustomerId { get; set; }
    public string DisplayName { get; set; } = "";
    public long Version { get; set; }
}

internal sealed class UpdateCustomerSnapshot(OrderingDbContext db)
    : IIntegrationEventHandler<CustomerRenamedIntegrationEvent>
{
    public async Task HandleAsync(CustomerRenamedIntegrationEvent e, CancellationToken ct)
    {
        var snapshot = await db.CustomerSnapshots.FindAsync([e.CustomerId], ct);
        if (snapshot is null)
            db.CustomerSnapshots.Add(new() { CustomerId = e.CustomerId, DisplayName = e.DisplayName, Version = e.Version });
        else if (e.Version > snapshot.Version)                 // ignoruj starší / duplicitní události
            (snapshot.DisplayName, snapshot.Version) = (e.DisplayName, e.Version);

        await db.SaveChangesAsync(ct);
    }
}
```

Kopíruj jen pole, která modul skutečně potřebuje.

---

## Konzistence přes moduly

```
✗ Jedna transakce: vytvoř objednávku + odečti sklad + vytvoř fakturu
✓ Ordering: OrderConfirmed → Inventory: StockReserved / StockReservationFailed → Ordering: potvrdí / zruší
```

Vícekrokové procesy s kompenzací → saga (viz `event-driven/ed-messaging.md`).

---

## Cesta k mikroslužbě

Modul připravený k oddělení:
1. Komunikuje jen přes Contracts a integration events ✓
2. Má vlastní schéma bez FK ven ✓
3. Konzumenti jsou idempotentní ✓

Oddělení pak znamená: in-process transport → broker, synchronní `ICatalogApi` → HTTP/gRPC klient implementující stejné rozhraní, schéma → vlastní databáze. Business kód modulu se nemění.

✓ Odděluj až s konkrétním důvodem (škálování, nezávislé nasazování, jiný tým) — zapiš jako ADR.
