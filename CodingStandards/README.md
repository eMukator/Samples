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

Složka [claude](claude/) obsahuje subagenty, slash commandy a hooky pro řízený vývoj. Definice jsou obecné; projektová specifika (spuštění aplikace, účty, checklist reviewera, poučení) patří do projektového profilu.

| Soubor | Role |
|---|---|
| [commands/navrh.md](claude/commands/navrh.md) | `/navrh` – dva nezávislé návrhy řešení (architekt A ∥ B) a srovnání podle matice; vybírá člověk |
| [commands/manager.md](claude/commands/manager.md) | `/manager` – plán s akceptačními kritérii (schvaluje člověk), smyčka vývojář → reviewer ∥ tester (→ ui-ux), commit a úklid po každém úkolu, stavový soubor |
| [agents/architekt.md](claude/agents/architekt.md) | Návrh řešení nad skutečným stavem projektu (Opus) |
| [agents/vyvojar.md](claude/agents/vyvojar.md) | Implementuje jeden úkol včetně testů k AC; necommituje (Sonnet) |
| [agents/reviewer.md](claude/agents/reviewer.md) | Code review diffu proti pravidlům projektu a pokrytí AC testy; jen čte (Opus) |
| [agents/tester.md](claude/agents/tester.md) | Ověří chování proti AC – build, testy, běžící aplikace, prohlížeč (Sonnet) |
| [agents/ui-ux.md](claude/agents/ui-ux.md) | Posoudí UI z hlediska použitelnosti, konzistence a přístupnosti (Sonnet) |
| [profil.template.md](claude/profil.template.md) | Šablona projektového profilu `.claude/agentic/profil.md` |
| [hooks/](claude/hooks/) | `guard-bash.ps1` (zákaz skenerů, procesů v popředí, změny portu, příkazů z konfigurace), `guard-write.ps1` (chráněné cesty), `build-on-stop.ps1` (build po vývojáři, chyby mu vrátí), `cleanup.ps1` (osiřelé procesy) |

Lidské brány: výběr varianty (`/navrh`), schválení plánu, vytvoření migrace, eskalace po 3 kolech, push/merge.

### Instalace

Globálně (pro všechny projekty) zkopírovat do `~/.claude/` (na Windows `%USERPROFILE%\.claude\`):

```powershell
Copy-Item CodingStandards\claude\agents\*.md   $HOME\.claude\agents\
Copy-Item CodingStandards\claude\commands\*.md $HOME\.claude\commands\
New-Item -ItemType Directory -Force $HOME\.claude\hooks | Out-Null
Copy-Item CodingStandards\claude\hooks\*.ps1   $HOME\.claude\hooks\
Copy-Item CodingStandards\claude\hooks\hooks-config.user.json $HOME\.claude\hooks\hooks-config.json
```

Hooky zaregistrovat v `~/.claude/settings.json` (`cleanup.ps1` má v `hooks-config.json` výchozí `"dryRun": true` – nejdřív jen loguje do `.scratch/cleanup.log`):

```json
"hooks": {
  "PreToolUse": [
    { "matcher": "Bash", "hooks": [{ "type": "command", "command": "pwsh -NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"$HOME/.claude/hooks/guard-bash.ps1\"" }] },
    { "matcher": "Write|Edit|NotebookEdit", "hooks": [{ "type": "command", "command": "pwsh -NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"$HOME/.claude/hooks/guard-write.ps1\"" }] }
  ],
  "SubagentStop": [
    { "matcher": "vyvojar", "hooks": [{ "type": "command", "command": "pwsh -NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"$HOME/.claude/hooks/build-on-stop.ps1\"", "timeout": 300 }] },
    { "hooks": [{ "type": "command", "command": "pwsh -NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"$HOME/.claude/hooks/cleanup.ps1\"" }] }
  ],
  "Stop": [
    { "hooks": [{ "type": "command", "command": "pwsh -NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"$HOME/.claude/hooks/cleanup.ps1\"" }] }
  ]
}
```

V projektu:

1. `claude/profil.template.md` → `.claude/agentic/profil.md` a vyplnit.
2. `claude/hooks/hooks-config.example.json` → `.claude/hooks-config.json` a upravit (port, zakázané příkazy, chráněné cesty, `buildOnStop`).
3. Do `.gitignore` přidat `/.claude/runs/` a `/.scratch/`.

Projektový `.claude/agents/<jméno>.md` přebíjí globální – projektová specifika proto nepiš do kopií agentů, ale do profilu, jinak se globální opravy nepropíšou. Po instalaci restartovat Claude Code; kontrola přes `/agents` a `/`.

### Použití

1. Sepsat zadání do Markdown souboru, např. `zadani.md`.
2. U větší feature nejdřív `/navrh zadani.md`, vybrat variantu; zvolené řešení se zapíše do zadání.
3. `/manager zadani.md` – manažer kód nečte ani needituje: navrhne úkoly s akceptačními kritérii a čeká na schválení, pak volá `vyvojar`, souběžně `reviewer` a `tester` (u UI úkolů pak `ui-ux`), po úspěchu commitne a uklidí pracovní složku úkolu. Po 3 neúspěšných kolech se zastaví a čeká na instrukce.
4. Po přerušení (nová session, kompakce kontextu) `/manager zadani.md pokračuj` – naváže podle `.claude/runs/<slug>/stav.md`.
5. Na konci manažer navrhne poučení do profilu; zapíše je až po souhlasu.

Agenty lze volat i samostatně, např. „nech agenta reviewer projít poslední změny".

## Použití v promptu

```
Při implementaci dodržuj pravidla z CodingStandards/csharp/index.md a CodingStandards/nasa/index.md.
```
