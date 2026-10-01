# DDD — Persistence s EF Core

Doménový model nesmí znát EF Core. Veškeré mapování je v `Infrastructure` přes `IEntityTypeConfiguration<T>`.

## Mapování agregátu

```csharp
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.HasKey(o => o.Id);
        b.Property(o => o.Id).HasConversion(id => id.Value, v => new OrderId(v));
        b.Property(o => o.CustomerId).HasConversion(id => id.Value, v => new CustomerId(v));
        b.Property(o => o.Status).HasConversion<string>().HasMaxLength(32);

        // Vnitřní entity agregátu — vlastněné, ukládají se s rootem
        b.OwnsMany(o => o.Lines, line =>
        {
            line.WithOwner().HasForeignKey("OrderId");
            line.Property<int>("Id");                 // shadow key
            line.HasKey("Id");
            line.Property(l => l.ProductId).HasConversion(id => id.Value, v => new ProductId(v));
            line.OwnsOne(l => l.UnitPrice, m =>
            {
                m.Property(x => x.Amount).HasColumnName("UnitPrice").HasPrecision(18, 2);
                m.Property(x => x.Currency).HasColumnName("Currency").HasMaxLength(3);
            });
        });
        b.Navigation(o => o.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);   // zapisuje do _lines

        b.Ignore(o => o.Total);         // počítaná property
        b.Ignore(o => o.DomainEvents);
    }
}
```

- Value object s jednou hodnotou → `HasConversion`.
- Value object s více hodnotami → `ComplexProperty` (EF Core 8+) nebo `OwnsOne`.
- Pro mnoho strongly-typed ID použij konvenci v `ConfigureConventions` místo opakování `HasConversion`.

```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder cb)
{
    cb.Properties<OrderId>().HaveConversion<OrderIdConverter>();
    cb.Properties<CustomerId>().HaveConversion<CustomerIdConverter>();
}

public sealed class OrderIdConverter() : ValueConverter<OrderId, Guid>(id => id.Value, v => new OrderId(v));
```

---

## Repository — per aggregate, bez SaveChanges

```csharp
// Domain nebo Application — interface jen pro aggregate root
public interface IOrderRepository
{
    Task<Order?> GetAsync(OrderId id, CancellationToken ct);
    void Add(Order order);
}

// Infrastructure
public sealed class OrderRepository(AppDbContext db) : IOrderRepository
{
    public Task<Order?> GetAsync(OrderId id, CancellationToken ct)
        => db.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);   // tracking! agregát se bude měnit; owned typy se načtou automaticky

    public void Add(Order order) => db.Orders.Add(order);
}
```

```csharp
// ✗ Špatně
public interface IRepository<T> { IQueryable<T> Query(); Task UpdateAsync(T e); Task SaveAsync(); }
// - IQueryable prosakuje persistence do aplikace
// - UpdateAsync je zbytečné (change tracker)
// - SaveAsync v repository rozbíjí transakci přes více repozitářů
```

- **Žádné `Update()` metody** — EF change tracker změny zachytí.
- **`SaveChanges` volá Unit of Work** na konci handleru, ne repository.
- Repository vrací **celý agregát**. Pro čtení do UI použij query side (`cqrs/`), ne repository.

---

## Unit of Work

```csharp
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);
}

// AppDbContext implementuje IUnitOfWork přímo — žádná wrapper třída
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Order> Orders => Set<Order>();

    async Task IUnitOfWork.SaveChangesAsync(CancellationToken ct) => await SaveChangesAsync(ct);
}

builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
```

---

## Dispatch domain events — interceptor

Události se dispatchují **před dokončením `SaveChanges`** ve stejné transakci → handlery uvnitř bounded contextu jsou konzistentní (vše, nebo nic).

```csharp
public sealed class DomainEventsInterceptor(IDomainEventDispatcher dispatcher) : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        if (eventData.Context is null) return result;

        // Smyčka: handler může vyvolat další události
        for (var round = 0; round < 10; round++)            // ochrana proti cyklu (nasa/rule-01)
        {
            var aggregates = eventData.Context.ChangeTracker
                .Entries<IHasDomainEvents>()
                .Select(e => e.Entity)
                .Where(e => e.DomainEvents.Count > 0)
                .ToList();
            if (aggregates.Count == 0) return result;

            var events = aggregates.SelectMany(a => a.DomainEvents).ToList();
            aggregates.ForEach(a => a.ClearDomainEvents());

            foreach (var e in events)
                await dispatcher.DispatchAsync(e, ct);
        }
        throw new InvalidOperationException("Domain events nekonvergují — pravděpodobně cyklus handlerů.");
    }
}
```

- `IHasDomainEvents` viz `ddd-tactical.md`.
- Interceptor registruj přes `options.AddInterceptors(sp.GetRequiredService<DomainEventsInterceptor>())` v `AddDbContext((sp, options) => …)`.
- Handler domain eventu **nevolá `SaveChanges`** — běží uvnitř probíhajícího uložení.
- Reakce mimo transakci (e-mail, jiná služba, jiný bounded context) → v handleru zapiš **integration event do outboxu** (viz `event-driven/ed-outbox-inbox.md`).

---

## Optimistická concurrency na agregátu

Každý aggregate root má concurrency token (viz `database-ef/db-ef-patterns.md`). Konflikt → `409 Conflict`, nikdy tiché přepsání.
