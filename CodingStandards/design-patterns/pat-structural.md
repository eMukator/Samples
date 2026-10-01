# Strukturální vzory

## Decorator

Přidá chování (logování, cache, retry, metriky) **bez úpravy** původní třídy.

```csharp
public interface IProductCatalog
{
    Task<ProductDto?> GetAsync(ProductId id, CancellationToken ct);
}

internal sealed class ProductCatalog(AppDbContext db) : IProductCatalog { ... }

// ✓ Caching decorator
internal sealed class CachedProductCatalog(IProductCatalog inner, HybridCache cache) : IProductCatalog
{
    public async Task<ProductDto?> GetAsync(ProductId id, CancellationToken ct)
        => await cache.GetOrCreateAsync($"product:{id}", async t => await inner.GetAsync(id, t), cancellationToken: ct);
}

// Registrace přes Scrutor
builder.Services.AddScoped<IProductCatalog, ProductCatalog>();
builder.Services.Decorate<IProductCatalog, CachedProductCatalog>();
```

- Generické decoratory pro handlery viz `cqrs/cqrs-pipeline.md`.
- **Kdy ne:** jediné místo použití — přidej chování přímo. ✗ Decorator dědičností (`class CachedCatalog : ProductCatalog`).

---

## Adapter

Převede cizí rozhraní na naše. V DDD terminologii součást **Anti-Corruption Layer** (viz `ddd/ddd-strategic.md`).

```csharp
// Náš port
public interface ISmsSender { Task SendAsync(PhoneNumber to, string text, CancellationToken ct); }

// Adapter na SDK dodavatele
internal sealed class TwilioSmsSender(TwilioRestClient client, IOptions<TwilioOptions> options) : ISmsSender
{
    public Task SendAsync(PhoneNumber to, string text, CancellationToken ct)
        => MessageResource.CreateAsync(to: new(to.Value), from: new(options.Value.From), body: text, client: client);
}
```

✗ SDK typy (`MessageResource`, `StripeCharge`) mimo adapter.

---

## Facade

Jednoduché rozhraní nad složitým subsystémem.

```csharp
// ✓ Modul vystavuje fasádu přes Contracts (viz modular-monolith/mm-communication.md)
public interface ICatalogApi { Task<ProductPriceDto?> GetPriceAsync(Guid productId, CancellationToken ct); }
```

**Kdy ne:** ✗ „Service" fasáda, která jen 1:1 přeposílá volání do jiné služby — skok navíc bez hodnoty.

---

## Proxy

Stejné rozhraní, řízení přístupu: lazy loading, vzdálené volání, autorizace.

```csharp
// ✓ Lazy<T> pro drahou inicializaci — vestavěný „virtual proxy"
private readonly Lazy<Task<SearchIndex>> _index = new(() => SearchIndex.LoadAsync());

// ✓ Typed HTTP client implementující doménový port = remote proxy
internal sealed class RemoteCatalogApi(HttpClient http) : ICatalogApi { ... }
```

---

## Composite

Strom objektů se stejným rozhraním pro list i větev.

```csharp
// ✓ Pravidla slev — jedno pravidlo nebo kombinace
public interface IDiscountRule { Money Apply(Order order, Money price); }

public sealed class CompositeDiscount(IEnumerable<IDiscountRule> rules) : IDiscountRule
{
    public Money Apply(Order order, Money price) => rules.Aggregate(price, (p, rule) => rule.Apply(order, p));
}
```

**Kdy ne:** plochý seznam bez vnoření → stačí `foreach`.

---

## Repository

Viz `ddd/ddd-persistence.md` (per aggregate, bez `SaveChanges`) a `csharp/architecture.md` (CRUD varianta).
✗ Generický `IRepository<T>` nad EF Core — `DbSet<T>` už repository je.
