# Výkon — cache a I/O

## Volba cache

| Typ | Kdy |
|-----|-----|
| **`HybridCache`** (.NET 9+) | ✓ Výchozí pro data aplikace — L1 v paměti + volitelně L2 (Redis), ochrana proti stampede, tagy |
| Output cache | Celé HTTP odpovědi veřejných / málo personalizovaných endpointů |
| `IMemoryCache` | Starší projekty; nový kód → HybridCache |
| HTTP cache hlavičky (`Cache-Control`, ETag) | Statické soubory, veřejná API — cache v prohlížeči / CDN |

---

## HybridCache

```csharp
builder.Services.AddHybridCache(o =>
{
    o.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(10),          // L2 (distribuovaná)
        LocalCacheExpiration = TimeSpan.FromMinutes(2)  // L1 (paměť instance)
    };
});
// L2: pokud je registrován IDistributedCache (např. AddStackExchangeRedisCache), použije se automaticky

internal sealed class GetProductHandler(HybridCache cache, AppDbContext db)
{
    public ValueTask<ProductDto?> HandleAsync(ProductId id, CancellationToken ct)
        => cache.GetOrCreateAsync(
            $"product:{id}",
            async token => await db.Products.AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new ProductDto(p.Id.Value, p.Name, p.Price.Amount))
                .FirstOrDefaultAsync(token),
            tags: ["products"],
            cancellationToken: ct);
}

// Invalidace po změně
await cache.RemoveAsync($"product:{id}", ct);
await cache.RemoveByTagAsync("products", ct);
```

Pravidla:
- Klíč obsahuje **vše, na čem výsledek závisí** (ID, jazyk, tenant, role) — jinak únik dat mezi uživateli.
- Každá cache má definovanou **invalidaci** (po změně, tagem) nebo vědomě akceptovanou zastaralost (TTL).
- Cachuj DTO, ne EF entity (tracking, lazy loading, velikost).
- ✗ Cache jako náplast na pomalý dotaz bez indexu — oprav nejdřív dotaz.
- Invalidaci po změně dělej v handleru domain eventu nebo po `SaveChanges`, ne ručně na deseti místech.

---

## Output cache

```csharp
builder.Services.AddOutputCache(o =>
{
    o.AddPolicy("catalog", p => p.Expire(TimeSpan.FromMinutes(5)).Tag("catalog").SetVaryByQuery("page", "category"));
});

app.UseOutputCache();   // po UseRouting / UseAuthentication

app.MapGet("/api/v1/catalog", GetCatalog).CacheOutput("catalog");

// Invalidace
await outputCacheStore.EvictByTagAsync("catalog", ct);
```

- Výchozí policy **necachuje** požadavky s autentizací a odpovědi se `Set-Cookie` — nevypínej to bez rozmyslu.
- Více instancí → Redis store (`AddStackExchangeRedisOutputCache`).

---

## Streamování velkých výsledků

```csharp
// ✗ Celý export v paměti
var all = await db.Orders.AsNoTracking().ToListAsync(ct);   // 2 mil. řádků
return Results.Ok(all);

// ✓ IAsyncEnumerable — System.Text.Json serializuje průběžně
app.MapGet("/api/v1/orders/export", (AppDbContext db, CancellationToken ct) =>
    db.Orders.AsNoTracking()
        .Select(o => new OrderExportRow(o.Id.Value, o.CreatedAt, o.Status.ToString()))
        .AsAsyncEnumerable());

// ✓ Soubory — stream, ne byte[]
return Results.File(await storage.OpenReadAsync(path, ct), "application/pdf", fileName);
```

- API seznamy vždy stránkované (viz `api-design/`); streamování je pro exporty a integrace.
- Upload: čti `Request.Body` / `IFormFile.OpenReadStream()` jako stream, ne `ReadAllBytes`.

---

## Komprese a statické soubory

```csharp
// .NET 9+ — fingerprinting, předkomprimované gzip/brotli soubory při buildu, správné cache hlavičky
app.MapStaticAssets();

// Dynamické odpovědi
builder.Services.AddResponseCompression(o =>
{
    o.EnableForHttps = true;   // ⚠ BREACH: nekomprimuj odpovědi, které míchají tajemství (CSRF token) s uživatelským vstupem
    o.Providers.Add<BrotliCompressionProvider>();
    o.Providers.Add<GzipCompressionProvider>();
});
app.UseResponseCompression();
```

Za reverse proxy / CDN, která komprimuje sama → kompresi v aplikaci vypni.

---

## Databáze a síť

- DB: viz `database-ef/` — projekce, `AsNoTracking`, indexy, N+1, compiled queries, `ExecuteUpdate`.
- Paralelní nezávislá I/O volání: `await Task.WhenAll(a, b)` — ✗ ale ne na stejném `DbContext` (není thread-safe).
- HTTP klienti: viz `resilience/res-http-clients.md` (pooling přes `IHttpClientFactory`).
- Background práce mimo request (e-maily, exporty) → fronta / Outbox, request vrátí `202 Accepted`.
