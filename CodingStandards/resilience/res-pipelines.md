# Resilience — pipelines mimo HTTP, databáze

## Pojmenovaná pipeline (Polly v8 + DI)

Pro SDK klienty (SMTP, blob storage, broker, gRPC), které nejdou přes `HttpClientFactory`.

```csharp
// Microsoft.Extensions.Resilience
builder.Services.AddResiliencePipeline("smtp", b => b
    .AddRetry(new RetryStrategyOptions
    {
        ShouldHandle = new PredicateBuilder().Handle<SmtpException>().Handle<IOException>(),
        MaxRetryAttempts = 3,
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true,
        Delay = TimeSpan.FromSeconds(1)
    })
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5,
        MinimumThroughput = 10,
        SamplingDuration = TimeSpan.FromSeconds(30),
        BreakDuration = TimeSpan.FromSeconds(30)
    })
    .AddTimeout(TimeSpan.FromSeconds(10)));

internal sealed class SmtpEmailSender(ResiliencePipelineProvider<string> pipelines, ISmtpClient smtp) : IEmailSender
{
    private readonly ResiliencePipeline _pipeline = pipelines.GetPipeline("smtp");

    public async Task SendAsync(EmailMessage message, CancellationToken ct)
        => await _pipeline.ExecuteAsync(async token => await smtp.SendAsync(message, token), ct);
}
```

- Pipeline je **singleton** a thread-safe — circuit breaker sdílí stav napříč voláními (to je záměr).
- Pořadí strategií = pořadí přidání (první je vnější): retry obaluje circuit breaker, ten obaluje timeout jednoho pokusu.
- Telemetrie: Polly v8 publikuje metriky a logy automaticky při registraci přes DI → OpenTelemetry (viz `observability/`).

---

## Které chyby opakovat

| Opakovat ✓ | Neopakovat ✗ |
|-----------|--------------|
| Timeout, `HttpRequestException` (síť) | 400, 401, 403, 404, 422 |
| 408, 429 (s `Retry-After`), 500, 502, 503, 504 | Validační a business chyby |
| SQL transient (deadlock, failover) | Deserializační chyba, `ArgumentException` |
| Broker dočasně nedostupný | Porušení unique constraintu |

---

## EF Core — retry na připojení

```csharp
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connectionString, sql =>
    sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)));
```

S retry strategií **nesmí** být uživatelská transakce otevřena mimo execution strategy (EF vyhodí `InvalidOperationException`):

```csharp
// ✗ Špatně — s EnableRetryOnFailure selže
await using var tx = await db.Database.BeginTransactionAsync(ct);
...

// ✓ Správně — celý blok se při přechodné chybě zopakuje
var strategy = db.Database.CreateExecutionStrategy();
await strategy.ExecuteAsync(async () =>
{
    await using var tx = await db.Database.BeginTransactionAsync(ct);
    // ... více SaveChanges / raw SQL ...
    await tx.CommitAsync(ct);
});
```

- Blok uvnitř `ExecuteAsync` musí být **opakovatelný** — žádné externí volání (e-mail, HTTP) uvnitř.
- Jediné `SaveChangesAsync` bez explicitní transakce retry zvládne samo.
- PostgreSQL (Npgsql): `EnableRetryOnFailure()` funguje stejně.

---

## Vrstvení retry — pozor na násobení

```
✗ Blazor tlačítko (retry 3×) → API (Polly 3×) → HttpClient handler (3×) → služba
  = až 27 pokusů na jednu akci uživatele, při výpadku zesílíš zátěž (retry storm)
```

- Retry patří na **jednu** vrstvu — nejblíže k volané závislosti.
- Background zpracování (Outbox, konzument) má vlastní retry na úrovni zprávy → vnitřní pipeline jen krátký retry nebo žádný.

---

## Bulkhead / omezení souběhu

```csharp
// Max 10 souběžných volání drahé závislosti, dalších 20 čeká ve frontě, zbytek okamžitě odmítnut
builder.Services.AddResiliencePipeline("pdf-renderer", b => b
    .AddConcurrencyLimiter(permitLimit: 10, queueLimit: 20)
    .AddTimeout(TimeSpan.FromSeconds(30)));
```

Ochrana vlastních endpointů (příchozí rate limiting) viz `security/sec-rate-limiting.md`.
