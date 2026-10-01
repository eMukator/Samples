# Resilience — odolnost vůči selháním

Volání externích služeb, databáze a brokeru **selhávají**. Tato sada určuje, jak s tím počítat. Navazuje na `event-driven/`, `observability/`, `api-design/`.

## Moduly

@res-http-clients.md
@res-pipelines.md
@res-idempotency.md

## Rychlá kontrola

- [ ] HTTP klienti výhradně přes `IHttpClientFactory` (typed clients), nikdy `new HttpClient()` per volání
- [ ] Každý typed client má resilience handler (`AddStandardResilienceHandler` nebo vlastní pipeline)
- [ ] Každé externí volání má timeout a propaguje `CancellationToken`
- [ ] Retry jen pro přechodné chyby a idempotentní operace; exponenciální backoff + jitter
- [ ] Retry na jedné vrstvě — ne HTTP retry + Polly retry + broker retry naráz (násobení)
- [ ] Circuit breaker u závislostí, jejichž výpadek nesmí shodit nás
- [ ] Neidempotentní POST endpointy přijímají `Idempotency-Key`
- [ ] EF Core: `EnableRetryOnFailure` + explicitní transakce přes execution strategy
- [ ] Degradace: při výpadku nekritické závislosti fallback (cache, výchozí hodnota), ne 500
- [ ] Selhání a otevřené circuit breakery jsou v metrikách a mají alert

## Kdy NEpřidávat

- Retry na lokální, deterministické operace (validace, výpočet) — selže znovu stejně.
- Vlastní retry smyčky — vždy Polly / Microsoft.Extensions.Resilience.
