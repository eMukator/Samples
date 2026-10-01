# CodingStandards

Sady pravidel (Markdown) pro promptování AI asistentů. Stačí v promptu odkázat na `index.md` příslušné sady. Každý `index.md` obsahuje rychlou kontrolu a seznam modulů. Přehled všech sad je v [index.md](index.md).

## Backend / .NET

| Sada | Obsah |
|---|---|
| [csharp](csharp/index.md) | Pojmenování (`_camelCase` fieldy, PascalCase konstanty), async/await bez `async void`, nullable reference types, žádné magic strings, specifické výjimky, architektura, unit testy s fake implementacemi místo mocků |
| [api-design](api-design/index.md) | REST URL v kebab-case, HTTP status kódy, RFC 9457 ProblemDetails, cursor paginace, FluentValidation, verzování v URL |
| [database-ef](database-ef/index.md) | EF Core migrace, indexy na FK, soft delete přes query filter, audit columns, AsNoTracking, žádné N+1 |
| [blazor](blazor/index.md) | Interactive Server: vědomý render mode, prerendering bez dvojího načítání, DbContext přes factory, žádný HttpContext v komponentách, dispose odběrů, InvokeAsync, sticky sessions |
| [environment-config](environment-config/index.md) | User Secrets / Key Vault, strongly-typed options s ValidateOnStart, Docker jako non-root, health probes |
| [observability](observability/index.md) | Serilog se strukturovaným logováním, bez PII, CorrelationId, health checks, OpenTelemetry |
| [performance](performance/index.md) | Nejdřív měřit (dotnet-counters, BenchmarkDotNet), async až dolů, alokace v hot paths (Span, ArrayPool), FrozenDictionary, source generatory, HybridCache, output cache, streamování |
| [resilience](resilience/index.md) | IHttpClientFactory + `AddStandardResilienceHandler`, retry jen pro přechodné chyby, circuit breaker, timeouty, EF execution strategy, Idempotency-Key, fallback |
| [email-notifications](email-notifications/index.md) | Outbox pattern, plain-text alternativa, unsubscribe, SPF/DKIM/DMARC, hard bounce |
| [security](security/index.md) | OWASP Top 10: injection, CSP/HSTS hlavičky, CSRF, CORS, rate limiting, bezpečné uploady, závislosti |

## Architektura & návrh

| Sada | Obsah |
|---|---|
| [design-principles](design-principles/index.md) | SOLID v moderním C#, composition over inheritance, Law of Demeter, feature cohesion, YAGNI / KISS / DRY, kdy interface s jednou implementací |
| [design-patterns](design-patterns/index.md) | GoF vzory idiomaticky v .NET 8+ (DI, keyed services, Scrutor decorator, specification, state, middleware), ke každému „kdy ne" |
| [clean-architecture](clean-architecture/index.md) | Rozhodovací tabulka stylů, závislosti dovnitř, porty a adaptéry, `internal` jako výchozí, architektonické testy (NetArchTest) |
| [ddd](ddd/index.md) | Bohatý model bez public setterů, value objects jako record, strongly-typed IDs, malé agregáty, repository per aggregate, domain events, bounded contexts a ACL |
| [cqrs](cqrs/index.md) | Command vs. query, jeden handler = jeden use case, decoratory pro validaci a logování, vertical slices, MediatR/AutoMapper jen po licenčním rozhodnutí |
| [modular-monolith](modular-monolith/index.md) | Modul = bounded context, vlastní DbContext a schéma, komunikace přes `*.Contracts` a integration events, bez transakcí přes moduly, cesta k mikroslužbě |
| [event-driven](event-driven/index.md) | Domain vs. integration events, aditivní verzování kontraktů, Outbox/Inbox, retry + DLQ, choreografie vs. sagy, event sourcing jen vědomě |
| [adr](adr/index.md) | Architecture Decision Records v `docs/adr/`, šablona, přijatý ADR se nemění, ale nahrazuje; AI agent ADR čte před architektonickou změnou |

## Frontend / web

| Sada | Obsah |
|---|---|
| [javascript](javascript/index.md) | TypeScript strict, `const` jako výchozí, žádné `any`, ošetřené async chyby, řazení importů |
| [tailwind](tailwind/index.md) | Mobile-first třídy, bez arbitrárních hodnot, kontejnery, obrázky s aspect-*, motion-safe |
| [responsive-pwa](responsive-pwa/index.md) | Mobile-first, touch targety 44 px, manifest, service worker, formuláře, Lighthouse PWA ≥ 90 |
| [seo](seo/index.md) | Title + meta description, canonical, jeden H1, sitemap, Core Web Vitals, strukturovaná data, a11y |

## Obecné

| Sada | Obsah |
|---|---|
| [nasa](nasa/index.md) | NASA JPL 10 pravidel (Holzmann) adaptovaná pro C#/JS: omezené smyčky, krátké funkce, assertions, kontrola návratových hodnot |
| [testing-advanced](testing-advanced/index.md) | Testovací pyramida, unit vs. integrační test podle vrstvy, fake místo mocků, determinismus (FakeTimeProvider), Builder, WebApplicationFactory, Testcontainers, bUnit pro Blazor, coverage po vrstvách, mutační testy |
| [git-cicd](git-cicd/index.md) | Conventional Commits, prefixy větví, squash merge, CI pipeline, žádné secrets v commitu |
| [external-apis](external-apis/index.md) | Reference externích služeb (Mapy.com Tile API: URL, jazyky, rate limit) |

## Prompty

- [CLAUDE.prompt.md](CLAUDE.prompt.md): prompt pro vygenerování stručného `CLAUDE.md` k repozitáři (průzkum repa, otázky, max ~60 řádků).

## Claude Code: agenti a manažer

Složka [claude](claude/) obsahuje subagenty a slash command pro řízený vývoj:

| Soubor | Role |
|---|---|
| [agents/vyvojar.md](claude/agents/vyvojar.md) | Implementuje jeden přesně vymezený úkol |
| [agents/tester.md](claude/agents/tester.md) | Nezávisle ověří splnění úkolu (funkčně i kvalitativně) |
| [agents/ui-ux.md](claude/agents/ui-ux.md) | Posoudí UI z hlediska použitelnosti, konzistence a přístupnosti |
| [commands/manager.md](claude/commands/manager.md) | Rozloží zadání na úkoly a řídí smyčku vývojář → tester (→ ui-ux), commit po každém úkolu |

### Instalace

Globálně (pro všechny projekty) zkopírovat do `~/.claude/` (na Windows `%USERPROFILE%\.claude\`):

```powershell
Copy-Item CodingStandards\claude\agents\*.md   $HOME\.claude\agents\
Copy-Item CodingStandards\claude\commands\*.md $HOME\.claude\commands\
```

Jen pro jeden projekt zkopírovat do `.claude/agents/` a `.claude/commands/` v kořeni daného repa. Po zkopírování restartovat Claude Code; kontrola přes `/agents` (výpis agentů) a `/` (nabídka commandů).

### Použití

1. Sepsat zadání do Markdown souboru, např. `zadani.md`.
2. V Claude Code spustit:
   ```
   /manager zadani.md
   ```
3. Manažer sám kód nečte ani needituje: rozloží zadání na úkoly, volá `vyvojar`, výsledek ověří `tester` (u UI úkolů i `ui-ux`), po úspěchu nechá commitnout. Po 3 neúspěšných pokusech na stejném úkolu se zastaví a čeká na instrukce.

Agenty lze volat i samostatně, např. „nech agenta tester ověřit poslední změny".

## Použití v promptu

```
Při implementaci dodržuj pravidla z CodingStandards/csharp/index.md a CodingStandards/nasa/index.md.
```
