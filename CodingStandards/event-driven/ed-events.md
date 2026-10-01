# Události — typy, kontrakty, dispatch

## Domain event vs. Integration event

| | Domain event | Integration event |
|-|--------------|-------------------|
| Rozsah | Uvnitř bounded contextu | Mezi moduly / službami |
| Typy v payloadu | Doménové (`OrderId`, `Money`) | Primitivní a serializovatelné (`Guid`, `decimal`, `string`) |
| Doručení | In-process, ve stejné transakci | Přes Outbox → broker, asynchronně, at-least-once |
| Umístění | `*.Domain` | `*.Contracts` (sdílený NuGet / projekt) |
| Změna | Libovolně (interní) | Jen aditivně, verzováním |

```csharp
// Domain — interní, bohaté typy
public sealed record OrderConfirmed(OrderId OrderId, Money Total) : IDomainEvent;

// Ordering.Contracts — veřejný kontrakt
public sealed record OrderConfirmedIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    string Currency) : IIntegrationEvent;
```

✗ Nikdy nepublikuj doménovou událost ven přímo — zamkneš si tím vnitřní model.

---

## Obálka zprávy (metadata)

```csharp
public interface IIntegrationEvent;

public sealed record MessageEnvelope(
    Guid MessageId,          // pro deduplikaci (Guid.CreateVersion7())
    string Type,             // "ordering.order-confirmed.v1"
    DateTimeOffset OccurredAt,
    string? CorrelationId,   // z Activity.Current?.TraceId / CorrelationId middleware
    string? CausationId,     // MessageId zprávy, která tuto způsobila
    string Payload);         // JSON
```

- `Type` je **stabilní řetězec**, ne `typeof(T).FullName` — přejmenování třídy nesmí rozbít konzumenty.
- Metadata patří do hlaviček zprávy, payload obsahuje jen business data.

---

## Pravidla pro obsah události

```csharp
// ✗ Příliš tlustá — celý agregát, PII, interní stav
public sealed record CustomerUpdated(Customer Customer);

// ✗ Příliš hubená — konzument musí volat zpět („chatty" integrace)
public sealed record OrderConfirmed(Guid OrderId);

// ✓ Dostatečná pro typické konzumenty, bez zbytečných dat
public sealed record OrderConfirmedIntegrationEvent(Guid OrderId, Guid CustomerId, decimal TotalAmount, string Currency);
```

- Událost popisuje **business fakt**, ne CRUD změnu (`OrderShipped`, ne `OrderUpdated`).
- PII posílej jen pokud ji konzument nutně potřebuje (GDPR — data v brokeru a DLQ se těžko mažou).

---

## Verzování kontraktů

| Změna | Povoleno? |
|-------|-----------|
| Přidání nové nepovinné property | ✓ (konzument ji ignoruje) |
| Přejmenování / odebrání property | ✗ |
| Změna typu property | ✗ |
| Změna významu hodnoty | ✗ |
| Breaking change | Nový typ `OrderConfirmedV2`, publikuj **oba** do migrace všech konzumentů |

```csharp
// Deserializace tolerantní k neznámým polím (výchozí v System.Text.Json) — nezapínej
// JsonUnmappedMemberHandling.Disallow na konzumentech integration events.
```

### Testy kontraktů — pravidla verzování vynucená testem

Pravidla z tabulky výše hlídají dva testy pro každý integration event v projektu `*.Contracts.Tests`:

```csharp
// dotnet add package Verify.Xunit
public sealed class OrderConfirmedContractTests
{
    private static readonly OrderConfirmedIntegrationEvent Sample = new(
        OrderId: Guid.Parse("0192f4c1-7a3e-7b8a-9c1d-2e3f4a5b6c7d"),   // pevné hodnoty — determinismus
        CustomerId: Guid.Parse("0192f4c1-7a3e-7b8a-9c1d-000000000001"),
        TotalAmount: 1250.50m,
        Currency: "CZK");

    // 1. Snapshot — jakákoli změna serializovaného tvaru shodí test a ukáže diff
    [Fact]
    public Task Serialization_MatchesApprovedContract()
        => VerifyJson(JsonSerializer.Serialize(Sample, ContractJson.Options));

    // 2. Zpětná kompatibilita — JSON publikovaný starší verzí se musí dát přečíst
    [Theory]
    [InlineData("order-confirmed.v1.json")]
    public void Deserialize_PreviousVersionPayload_Succeeds(string fixture)
    {
        var json = File.ReadAllText(Path.Combine("Fixtures", fixture));

        var e = JsonSerializer.Deserialize<OrderConfirmedIntegrationEvent>(json, ContractJson.Options);

        e.Should().NotBeNull();
        e!.OrderId.Should().NotBeEmpty();
    }
}
```

- Schválený snapshot (`*.verified.txt`) se commituje. Změna snapshotu v PR = změna kontraktu → reviewer ověří, že je **aditivní** (tabulka výše); breaking change jen jako nový typ `…V2`.
- Při každém vydání nové verze kontraktu ulož ukázkový payload do `Fixtures/` — testy zpětné kompatibility se tím rozšíří o další verzi.
- `ContractJson.Options` = **stejné** `JsonSerializerOptions`, jaké používá publisher (naming policy, enum converter); jinak test ověřuje jiný tvar, než jde do brokeru.

---

## In-process dispatch domain events

```csharp
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken ct);
}

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken ct);
}

// Reflexe je zde nutná (typ události známe až za běhu) — nasa/rule-07: jen infrastruktura, výsledek cachovaný
internal sealed class DomainEventDispatcher(IServiceProvider sp) : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, (Type HandlerType, MethodInfo Method)> HandlerCache = new();

    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken ct)
    {
        var (handlerType, method) = HandlerCache.GetOrAdd(domainEvent.GetType(), eventType =>
        {
            var type = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
            return (type, type.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!);
        });

        foreach (var handler in sp.GetServices(handlerType))
            await (Task)method.Invoke(handler, BindingFlags.DoNotWrapExceptions, null, [domainEvent, ct], null)!;
    }
}

// Registrace handlerů přes Scrutor
services.Scan(s => s.FromAssemblyOf<DomainEventDispatcher>()
    .AddClasses(c => c.AssignableTo(typeof(IDomainEventHandler<>)), publicOnly: false)
    .AsImplementedInterfaces().WithScopedLifetime());
services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
```

Volá ho `DomainEventsInterceptor` (viz `ddd/ddd-persistence.md`).

---

## Domain event → integration event

```csharp
// Handler domain eventu jen přeloží a zapíše do outboxu — běží ve stejné transakci
internal sealed class PublishOrderConfirmed(IOutbox outbox) : IDomainEventHandler<OrderConfirmed>
{
    public Task HandleAsync(OrderConfirmed e, CancellationToken ct)
    {
        outbox.Add(new OrderConfirmedIntegrationEvent(e.OrderId.Value, /* … */ e.Total.Amount, e.Total.Currency));
        return Task.CompletedTask;
    }
}
```

---

## Neperzistentní fronta v procesu — `Channel<T>`

Pro „pošli na pozadí" uvnitř jedné instance (např. přegenerování náhledu) stačí `System.Threading.Channels`.

```csharp
builder.Services.AddSingleton(Channel.CreateBounded<ThumbnailRequest>(new BoundedChannelOptions(1000)
{
    FullMode = BoundedChannelFullMode.Wait
}));
```

⚠ Při restartu aplikace se obsah **ztratí**. Cokoli, co se nesmí ztratit → Outbox.
