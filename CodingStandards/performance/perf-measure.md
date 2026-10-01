# Výkon — měření

## Postup

1. **Definuj cíl** — „p95 endpointu `/orders` < 200 ms při 100 req/s", ne „rychlejší".
2. **Změř produkci / realistickou zátěž** — metriky a traces (OpenTelemetry, viz `observability/`) ukážou, *kde* se čas tráví.
3. **Profiluj** podezřelé místo.
4. **Mikrobenchmark** jen pro izolovaný kus kódu.
5. **Ověř zlepšení** stejným měřením jako v kroku 2.

---

## Nástroje

| Nástroj | Na co |
|---------|-------|
| OpenTelemetry traces | Kde v requestu se tráví čas (DB, HTTP, kód) |
| `dotnet-counters monitor -p <pid>` | Živě: CPU, GC, alokace, thread pool, výjimky, requesty |
| `dotnet-trace collect -p <pid>` | CPU profil (otevři v Visual Studio / PerfView / speedscope) |
| `dotnet-gcdump collect -p <pid>` | Co drží paměť (memory leak) |
| Visual Studio Profiler / JetBrains dotTrace, dotMemory | Interaktivní profilování |
| BenchmarkDotNet | Mikrobenchmark konkrétní metody |
| k6 / NBomber / Azure Load Testing | Zátěžové testy endpointů |
| EF Core logování SQL + `EXPLAIN` / execution plan | Pomalé dotazy |

```bash
dotnet tool install -g dotnet-counters
dotnet-counters monitor -n MyApp --counters System.Runtime,Microsoft.AspNetCore.Hosting
```

Varovné signály v counters:
- `ThreadPool Queue Length` roste → blokující volání (sync-over-async), thread pool starvation.
- `% Time in GC` > ~10 % → příliš alokací.
- `Gen 2 GC Count` roste rychle, paměť neklesá → leak nebo velké objekty (LOH > 85 KB).
- `Exception Count` vysoký → výjimky použité pro řízení toku.

---

## BenchmarkDotNet

```csharp
// Samostatný konzolový projekt benchmarks/MyApp.Benchmarks, spouštět v Release
[MemoryDiagnoser]
public class SlugBenchmarks
{
    private const string Input = "Příliš žluťoučký kůň úpěl ďábelské ódy";

    [Benchmark(Baseline = true)]
    public string Current() => SlugHelper.CreateSlug(Input);

    [Benchmark]
    public string Optimized() => SlugHelper.CreateSlugSpan(Input);
}

BenchmarkRunner.Run<SlugBenchmarks>();
```

```bash
dotnet run -c Release --project benchmarks/MyApp.Benchmarks
```

- Vždy `[MemoryDiagnoser]` — alokace jsou často důležitější než čas.
- Srovnávej s baseline, výsledky (tabulku) přilož do PR.
- ✗ Měření přes `Stopwatch` ve smyčce v Debug buildu — bezcenné (JIT, tiered compilation, GC).

---

## Zátěžové testy

- Testuj proti prostředí s produkční konfigurací (Release, stejná DB velikost řádově).
- Sleduj p50 / p95 / p99 latenci a error rate, ne průměr.
- Pro Blazor Server testuj počet souběžných okruhů (viz `blazor/blazor-hosting.md`).
