# Outbox & Inbox — spolehlivé doručení

## Problém dual write

```csharp
// ✗ Špatně — dva nezávislé zápisy
await uow.SaveChangesAsync(ct);
await bus.PublishAsync(new OrderConfirmedIntegrationEvent(...));   // pád zde = DB změněna, událost ztracena

// ✗ Špatně — opačné pořadí
await bus.PublishAsync(...);
await uow.SaveChangesAsync(ct);   // pád zde = událost odešla o změně, která neexistuje
```

✓ Řešení: zapiš událost do tabulky **Outbox ve stejné transakci** jako business změnu. Samostatný proces ji pak doručí.

---

## Doporučení: použij hotový outbox

| Knihovna | Poznámka |
|----------|----------|
| **Wolverine** (MIT) | Durable outbox/inbox pro EF Core, integrovaný s handlery |
| **MassTransit** | EF Core transactional outbox + inbox. v8 Apache 2.0, v9+ komerční licence |
| Vlastní implementace (níže) | Jen pokud nechceš broker knihovnu — např. jediný konzument, jednoduchá integrace |

---

## Vlastní minimální Outbox

```csharp
public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Type { get; init; }
    public required string Payload { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public string? CorrelationId { get; init; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}

// Mapování CLR typ ↔ stabilní název ("ordering.order-confirmed.v1"); implementace = FrozenDictionary v obou směrech
public interface IEventTypeRegistry
{
    string GetName(Type type);
    Type GetType(string name);
}

public interface IOutbox
{
    void Add(IIntegrationEvent integrationEvent);
}

internal sealed class EfOutbox(AppDbContext db, TimeProvider clock, IEventTypeRegistry types) : IOutbox
{
    public void Add(IIntegrationEvent e) => db.OutboxMessages.Add(new OutboxMessage
    {
        Type = types.GetName(e.GetType()),              // stabilní název, ne CLR typ
        Payload = JsonSerializer.Serialize(e, e.GetType()),
        OccurredAt = clock.GetUtcNow(),
        CorrelationId = Activity.Current?.TraceId.ToString()
    });
    // Žádné SaveChanges — uloží se s business změnou
}

// Konfigurace — filtrovaný index pro rychlé hledání nezpracovaných
b.HasIndex(m => m.OccurredAt).HasFilter("[ProcessedAt] IS NULL");
```

### Odesílač (BackgroundService)

```csharp
internal sealed class OutboxPublisher(
    IServiceScopeFactory scopes,
    IMessagePublisher publisher,
    TimeProvider clock,
    ILogger<OutboxPublisher> logger) : BackgroundService
{
    private const int BatchSize = 50;
    private const int MaxAttempts = 10;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2), clock);
        while (await timer.WaitForNextTickAsync(ct))
        {
            try { await PublishBatchAsync(ct); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            { logger.LogError(ex, "Outbox batch failed"); }
        }
    }

    private async Task PublishBatchAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var batch = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.Attempts < MaxAttempts)
            .OrderBy(m => m.OccurredAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        foreach (var message in batch)
        {
            try
            {
                await publisher.PublishAsync(message.Id, message.Type, message.Payload, message.CorrelationId, ct);
                message.ProcessedAt = clock.GetUtcNow();
            }
            catch (Exception ex)
            {
                message.Attempts++;
                message.LastError = ex.Message;
                logger.LogWarning(ex, "Outbox message {MessageId} failed (attempt {Attempt})", message.Id, message.Attempts);
            }
        }
        await db.SaveChangesAsync(ct);
    }
}
```

- ⚠ Strop: běží-li **více instancí**, zpracují stejné zprávy dvakrát (konzumenti jsou idempotentní, takže je to korektní, jen neefektivní). Při škálování zamykej řádky (`WITH (UPDLOCK, READPAST)` / `FOR UPDATE SKIP LOCKED`) nebo přejdi na knihovnu.
- Zprávy s `Attempts >= MaxAttempts` = alert (stejně jako DLQ).
- Zpracované zprávy mazat úlohou po N dnech (retence).
- Outbox **negarantuje pořadí** při chybách a retry — konzumenti s tím musí počítat.

---

## Inbox — idempotentní konzument

Broker doručuje **at-least-once** → stejná zpráva může přijít vícekrát. Konzument to musí ustát.

```csharp
public sealed class InboxMessage
{
    public required Guid MessageId { get; init; }
    public required string Consumer { get; init; }
    public DateTimeOffset ProcessedAt { get; init; }
}
// PK = (MessageId, Consumer)

internal sealed class OrderConfirmedConsumer(AppDbContext db, TimeProvider clock)
{
    public async Task HandleAsync(Guid messageId, OrderConfirmedIntegrationEvent e, CancellationToken ct)
    {
        const string consumer = nameof(OrderConfirmedConsumer);

        if (await db.InboxMessages.AnyAsync(m => m.MessageId == messageId && m.Consumer == consumer, ct))
            return;   // už zpracováno

        // … business reakce (např. vytvoření faktury) …

        db.InboxMessages.Add(new InboxMessage { MessageId = messageId, Consumer = consumer, ProcessedAt = clock.GetUtcNow() });
        await db.SaveChangesAsync(ct);   // business změna + záznam v inboxu atomicky
        // Souběžný duplikát skončí na PK violation → transakce se vrátí, zpráva se neprovede dvakrát
    }
}
```

### Přirozená idempotence (preferuj, kde jde)

```csharp
// ✓ Nastavení stavu je idempotentní samo o sobě
invoice.MarkAsPaid(paymentId);   // podruhé nic nezmění, pokud už je zaplaceno stejnou platbou

// ✗ Inkrementace idempotentní není
account.Balance += e.Amount;     // duplicita = dvojí připsání → vyžaduje Inbox
```

---

## Pořadí zpráv

- Předpokládej, že pořadí **není** garantované.
- Pokud na něm záleží: verze/sekvence v payloadu a ignoruj starší (`if (e.Version <= current.Version) return;`), nebo partition/session key u brokeru (Service Bus sessions, Kafka partition key = ID agregátu).
