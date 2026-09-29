# CodingStandards

Sady pravidel (Markdown) pro promptování AI asistentů. Stačí v promptu odkázat na `index.md` příslušné sady. Každý `index.md` obsahuje rychlou kontrolu a seznam modulů.

## Backend / .NET

| Sada | Obsah |
|---|---|
| [csharp](csharp/index.md) | Pojmenování, async/await bez `async void`, nullable reference types, žádné magic strings, specifické výjimky, architektura, unit testy |
| [api-design](api-design/index.md) | REST URL v kebab-case, HTTP status kódy, RFC 9457 ProblemDetails, cursor paginace, FluentValidation, verzování v URL |
| [database-ef](database-ef/index.md) | EF Core migrace, indexy na FK, soft delete přes query filter, audit columns, AsNoTracking, žádné N+1 |
| [environment-config](environment-config/index.md) | User Secrets / Key Vault, strongly-typed options s ValidateOnStart, Docker jako non-root, health probes |
| [observability](observability/index.md) | Serilog se strukturovaným logováním, bez PII, CorrelationId, health checks, OpenTelemetry |
| [email-notifications](email-notifications/index.md) | Outbox pattern, plain-text alternativa, unsubscribe, SPF/DKIM/DMARC, hard bounce |
| [security](security/index.md) | OWASP Top 10: injection, CSP/HSTS hlavičky, CSRF, CORS, rate limiting, bezpečné uploady, závislosti |

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
| [testing-advanced](testing-advanced/index.md) | Testovací pyramida, AAA, Builder pro testovací data, integrační testy, Testcontainers, fake místo mocků |
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
