# AI Coding Standards — přehled sad pravidel

Kořenový adresář: `c:\@DEV\_ai`

Každá podsložka obsahuje vlastní `index.md` s rychlou kontrolou a moduly.

---

## Sady pravidel

### [adr](adr/index.md) — Architecture Decision Records
Záznamy významných rozhodnutí v `docs/adr/`, šablona (kontext, varianty, rozhodnutí, důsledky), přijatý ADR se nemění, ale nahrazuje; AI agent ADR čte před architektonickou změnou.

### [api-design](api-design/index.md) — REST API design
URL jako podstatná jména v kebab-case, HTTP status kódy dle RFC, chybové odpovědi jako RFC 9457 ProblemDetails, cursor-based paginace, FluentValidation, verzování v URL prefixu, XML dokumentace endpointů.

### [blazor](blazor/index.md) — Blazor Interactive Server
Render mode vědomě (SSR vs. interaktivní), prerendering bez dvojího načítání, DbContext přes factory / scope na operaci, žádný HttpContext v komponentách, autorizace v handleru, dispose odběrů, InvokeAsync z cizích vláken, sticky sessions.

### [clean-architecture](clean-architecture/index.md) — Clean Architecture
Rozhodovací tabulka stylů (jednoduchý projekt → vrstvy → Clean → modulární monolit), závislosti dovnitř, porty a adaptéry, `internal` jako výchozí, architektonické testy (NetArchTest) a analyzéry v CI.

### [cqrs](cqrs/index.md) — CQRS & Vertical Slices
Command mění stav (vrací ID/Result), query čte projekcí bez repository, jeden handler = jeden use case, decoratory pro validaci a logování (Scrutor), bez MediatR/AutoMapper bez licenčního rozhodnutí, organizace podle feature.

### [csharp](csharp/index.md) — C# / ASP.NET Core
Pojmenování (PascalCase třídy, camelCase lokální), async/await bez `async void`, nullable reference types, žádné magic strings, výjimky specifické, unit testy pro každou business logiku.

### [database-ef](database-ef/index.md) — Databáze & EF Core
Migrace s popisným názvem, nikdy neupravovat po deployi, indexy na FK sloupcích, soft delete přes global query filter, audit columns, AsNoTracking pro read-only, žádné N+1 dotazy.

### [ddd](ddd/index.md) — Domain-Driven Design
Bohatý model bez public setterů, invarianty v doméně, value objects jako record, strongly-typed IDs, malé agregáty s odkazy přes ID, repository per aggregate bez SaveChanges, domain events přes interceptor, bounded contexts a ACL.

### [design-patterns](design-patterns/index.md) — Návrhové vzory
GoF vzory idiomaticky v .NET 8+: singleton/factory přes DI a keyed services, decorator přes Scrutor, adapter jako ACL, strategy, specification jako `Expression`, state tabulkou přechodů, chain of responsibility jako middleware. Ke každému „kdy ne".

### [design-principles](design-principles/index.md) — SOLID & pragmatismus
SOLID v moderním C# (keyed services, sealed), composition over inheritance, Law of Demeter, feature cohesion, YAGNI / KISS / DRY správně, kdy interface s jednou implementací ano a kdy ne.

### [email-notifications](email-notifications/index.md) — E-mail & notifikace
Odesílání mimo request pipeline (Outbox pattern), plain-text alternativa, unsubscribe link, CSS inline, SPF/DKIM/DMARC, správa hard bounců, sledování spam complaint rate.

### [environment-config](environment-config/index.md) — Prostředí & konfigurace
Secrets výhradně v User Secrets / Key Vault, strongly-typed options s ValidateOnStart, Docker jako non-root, HEALTHCHECK v Dockerfile, Kubernetes readiness/liveness proby.

### [git-cicd](git-cicd/index.md) — Git & CI/CD
Conventional Commits, větve s prefixem (feature/, fix/), squash merge, CI pipeline (build + testy + security scan), žádné secrets v commitu, větev smazána po merge.

### [javascript](javascript/index.md) — JavaScript / TypeScript
TypeScript strict mode, žádné `var`, `const` jako výchozí, žádné `any` bez komentáře, ošetřené async chyby, žádné `console.log` v produkci, řazení importů.

### [modular-monolith](modular-monolith/index.md) — Modulární monolit
Modul = bounded context, vlastní DbContext a DB schéma, ostatní moduly vidí jen `*.Contracts`, komunikace přes kontrakty a integration events, lokální kopie dat, bez transakcí přes moduly, test hranic v `.csproj`, cesta k mikroslužbě.

### [nasa](nasa/index.md) — NASA JPL 10 pravidel
Adaptace pravidel Gerard J. Holzmanna do C#/JS: žádné neomezené smyčky, funkce max 60 řádků, guard assertions, každá návratová hodnota zkontrolována, kompilace bez warningů, statická analýza.

### [observability](observability/index.md) — Logování & Observability
Serilog se strukturovaným výstupem, placeholders místo interpolace, žádná PII v logách, CorrelationId middleware, health checks /live + /ready, OpenTelemetry traces.

### [performance](performance/index.md) — Výkon .NET
Nejdřív měřit (dotnet-counters, traces, BenchmarkDotNet), async až dolů, alokace v hot paths (Span, ArrayPool, kapacity), FrozenDictionary, source generatory (Regex, LoggerMessage, JSON), HybridCache s invalidací, output cache, streamování.

### [resilience](resilience/index.md) — Resilience
IHttpClientFactory + `AddStandardResilienceHandler`, retry jen pro přechodné chyby a idempotentní operace, jedna vrstva retry, circuit breaker, timeouty s CancellationToken, EF execution strategy, Idempotency-Key pro POST, fallback u nekritických závislostí.

### [responsive-pwa](responsive-pwa/index.md) — Responzivní design & PWA
Mobile-first přístup, žádné fixní px šířky, touch targety min 44×44 px, web manifest, service worker s offline stavem, správné formulářové atributy, Lighthouse PWA ≥ 90.

### [security](security/index.md) — Bezpečnost (OWASP Top 10)
EF Core LINQ místo SQL řetězení, CSP/HSTS/X-Frame hlavičky, CSRF tokeny, CORS whitelist, rate limiting na auth, bezpečné uploady (magic bytes, mimo wwwroot), Dependabot.

### [seo](seo/index.md) — SEO & Web Standards
Unikátní title + meta description, canonical URL, alt atributy, jeden H1, aktuální sitemap, LCP < 2,5 s, CLS < 0,1, strukturovaná data, a11y soulad.

### [event-driven](event-driven/index.md) — Event-Driven Development
Domain vs. integration events, kontrakty verzované aditivně, Outbox ve stejné transakci, idempotentní konzumenti (Inbox), retry + DLQ, choreografie vs. sagy, event sourcing jen vědomě, propagace CorrelationId.

### [external-apis](external-apis/index.md) — Externí API — reference
Dokumentace a limity externích služeb: Mapy.com Tile API (tile URL, parametr `lang`, podporované jazykové kódy, rate limit).

### [tailwind](tailwind/index.md) — Tailwind CSS
Mobile-first třídy, žádné arbitrární hodnoty bez zdůvodnění, kontejner s max-w + mx-auto, obrázky s aspect-*, interaktivní prvky min 44px, animace v motion-safe:, vlastní hodnoty v tailwind.config.

### [testing-advanced](testing-advanced/index.md) — Testování — pokročilé vzory
Pyramida 70 % unit / 25 % integration / 5 % E2E, unit vs. integrační test podle vrstvy, doména přes doménové metody, fake implementace místo mocků, determinismus (FakeTimeProvider), Builder, WebApplicationFactory, Testcontainers (ne InMemory), bUnit pro Blazor, coverage po vrstvách, mutační testy domény.
