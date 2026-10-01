# Výkon .NET aplikací

**Nejdřív měř, pak optimalizuj.** Většina výkonových problémů je v I/O (DB, síť), ne v CPU. Navazuje na `database-ef/` (dotazy), `observability/` (metriky).

## Moduly

@perf-measure.md
@perf-memory-cpu.md
@perf-caching-io.md

## Rychlá kontrola

- [ ] Optimalizace podložená měřením (profil, BenchmarkDotNet, metriky) — ne odhadem
- [ ] Async až dolů, žádné `.Result` / `.Wait()` / `Task.Run` v request pipeline
- [ ] DB dotazy: projekce, `AsNoTracking`, žádné N+1, stránkování (viz `database-ef/`)
- [ ] Cache tam, kde se data čtou výrazně častěji, než mění (`HybridCache`, output cache) — s jasnou invalidací
- [ ] Velké výsledky streamované (`IAsyncEnumerable`, `Stream`), ne načtené celé do paměti
- [ ] Hot path bez zbytečných alokací: kapacita kolekcí, `StringBuilder`, žádné LINQ v těsných smyčkách
- [ ] `System.Text.Json` source generator a `[LoggerMessage]` v hot paths
- [ ] `[GeneratedRegex]` místo `new Regex(...)`
- [ ] Neměnné lookupy jako `FrozenDictionary` / `FrozenSet`
- [ ] Response compression + statické soubory přes `MapStaticAssets`

## Kdy NEoptimalizovat

- Kód mimo hot path (admin obrazovka volaná 10× denně) — čitelnost má přednost.
- Bez reprodukovatelného měření „před a po" — změna může výkon i zhoršit.
