# Resilience — HTTP klienti

## IHttpClientFactory + typed client

```csharp
// ✗ Špatně — vyčerpání socketů, ignorované DNS změny
public async Task<Rate> GetRateAsync()
{
    using var client = new HttpClient();
    return await client.GetFromJsonAsync<Rate>("https://api.cnb.cz/...");
}

// ✗ Špatně — statický HttpClient bez PooledConnectionLifetime ignoruje změny DNS
private static readonly HttpClient Client = new();
```

```csharp
// ✓ Správně — typed client
builder.Services.AddHttpClient<ExchangeRateClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["ExchangeRates:BaseUrl"]!);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MyApp/1.0");
    })
    .AddStandardResilienceHandler();

internal sealed class ExchangeRateClient(HttpClient http)
{
    public async Task<ExchangeRate?> GetRateAsync(string currency, CancellationToken ct)
    {
        using var response = await http.GetAsync($"rates/{Uri.EscapeDataString(currency)}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ExchangeRate>(ct);
    }
}
```

- Typed client je **transient** — neinjektuj ho do singletonu (captive dependency). Pro singleton použij `IHttpClientFactory.CreateClient(name)` per volání.
- Base URL a klíče z konfigurace / secrets (viz `environment-config/`).
- Odpověď cizího API = nedůvěryhodný vstup → validuj a mapuj na vlastní model (ACL, viz `ddd/ddd-strategic.md`).

---

## Standardní resilience handler

`Microsoft.Extensions.Http.Resilience` — `AddStandardResilienceHandler()` skládá (zvenku dovnitř):

| Strategie | Výchozí |
|-----------|---------|
| Rate limiter | 1000 souběžných požadavků |
| Total request timeout | 30 s (včetně všech retry) |
| Retry | 3×, exponenciálně s jitterem, jen přechodné chyby (5xx, 408, 429, `HttpRequestException`, timeout) |
| Circuit breaker | otevře se při ≥ 10 % chyb za 30 s (min. 100 požadavků), na 5 s |
| Attempt timeout | 10 s na jeden pokus |

```csharp
// Úprava pro pomalou službu
.AddStandardResilienceHandler(o =>
{
    o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(20);
    o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
    o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);   // musí být ≥ 2× AttemptTimeout
    o.Retry.MaxRetryAttempts = 2;
});

// Pro všechny klienty v aplikaci
builder.Services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());
```

- ⚠ Standardní retry opakuje i **POST**. Pro neidempotentní volání bez idempotency klíče retry vypni nebo omez na bezpečné metody:

```csharp
.AddStandardResilienceHandler(o => o.Retry.DisableForUnsafeHttpMethods());   // POST, PATCH, PUT, DELETE, CONNECT
```

- Respektuj `Retry-After` u 429/503 — standardní handler to dělá.
- Hedging (`AddStandardHedgingHandler`) jen pro idempotentní čtení s kritickou latencí a více replikami.

---

## Timeouty a zrušení

```csharp
// ✓ Token z requestu se propaguje až do HTTP volání — klient odejde → volání se zruší
app.MapGet("/rates/{currency}", (string currency, ExchangeRateClient client, CancellationToken ct)
    => client.GetRateAsync(currency, ct));
```

- `HttpClient.Timeout` (výchozí 100 s) nech vyšší než `TotalRequestTimeout` — timeout řídí pipeline.
- Timeout volání < timeout requestu volajícího. Jinak volající odejde a my dál čekáme.

---

## Degradace (fallback)

```csharp
public async Task<IReadOnlyList<Recommendation>> GetRecommendationsAsync(CustomerId id, CancellationToken ct)
{
    try
    {
        return await recommendationClient.GetAsync(id, ct);
    }
    catch (Exception ex) when (ex is HttpRequestException or BrokenCircuitException or TimeoutRejectedException)
    {
        logger.LogWarning(ex, "Recommendations unavailable, using fallback");
        return [];   // stránka funguje dál bez doporučení
    }
}
```

| Závislost | Při výpadku |
|-----------|-------------|
| Kritická (platby, sklad při objednávce) | Chyba uživateli s jasnou hláškou, žádný falešný úspěch |
| Nekritická (doporučení, statistiky, avatar) | Fallback: prázdný výsledek, poslední známá hodnota z cache |
| Asynchronní (e-mail, webhook) | Outbox + retry na pozadí (viz `event-driven/`) |
