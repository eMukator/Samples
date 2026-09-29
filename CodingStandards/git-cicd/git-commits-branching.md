# Git — Commit zprávy & Větvení

## Conventional Commits — formát

```
<typ>(<scope>): <popis>

[volitelné tělo]

[volitelné poznámky / BREAKING CHANGE]
```

### Typy

| Typ | Kdy použít | Bump verze |
|-----|-----------|-----------|
| `feat` | Nová funkce viditelná uživatelem | MINOR |
| `fix` | Oprava chyby | PATCH |
| `perf` | Zlepšení výkonu | PATCH |
| `refactor` | Refactoring bez změny chování | — |
| `test` | Přidání nebo úprava testů | — |
| `docs` | Pouze dokumentace | — |
| `style` | Formátování, mezery, čárky | — |
| `chore` | Závislosti, build, konfigurace | — |
| `ci` | CI/CD pipeline | — |
| `revert` | Vrácení commitu | — |

### Scope — název modulu nebo vrstvy

```bash
feat(orders): add bulk status update
fix(auth): prevent session fixation after login
perf(db): add composite index on Orders(CustomerId, Status)
refactor(payments): extract IPaymentGateway interface
test(orders): add integration tests for bulk update
chore(deps): update EF Core to 9.0.1
ci(github): add Trivy security scan
docs(api): document rate limiting headers in README
```

### Breaking change

```bash
# V subject s vykřičníkem
feat(api)!: change pagination to cursor-based

# V těle commitu
feat(api): change pagination to cursor-based

BREAKING CHANGE: offset-based pagination (?page=N&size=M) je odstraněn.
Použij cursor-based (?cursor=BASE64&size=M).
Migration guide: docs/migrations/v2-pagination.md
```

### Správné vs. špatné zprávy

```bash
# ✓ Dobré — jasné, konkrétní, akční
feat(orders): add email notification on status change
fix(cart): correct total calculation when applying percentage discount
perf(reports): cache monthly aggregations for 1 hour
chore(deps): upgrade Serilog from 3.1 to 4.0

# ✗ Špatné — vágní, neinformativní
fix stuff
WIP
changes
update
fix bug
refactor code
```

## Větvení — GitHub Flow

```
main
  └─ feature/order-bulk-update
  └─ fix/cart-total-rounding
  └─ perf/report-caching
  └─ chore/update-efcore-9
```

### Pravidla větví

- **Základ vždy `main`** — větve vycházejí z aktuálního `main`
- **Krátké větve** — maximálně 2–3 dny, pak merge konflikty rostou exponenciálně
- **Naming**: `feature/`, `fix/`, `perf/`, `refactor/`, `chore/`, `docs/`
- **Kebab-case**: `feature/order-bulk-update` ne `feature/OrderBulkUpdate`
- **Smaž po merge** — větve jsou dočasné

### Chráněný main

```yaml
# GitHub branch protection rules (Settings → Branches)
# Aplikuj na: main

required_status_checks:
  - Build and Test
  - Security Scan
require_pull_request_reviews:
  required_approving_review_count: 1
require_linear_history: true     # jen squash merge
restrict_pushes: true            # nikdo nemůže force-push
```

## Squash merge — standardní způsob mergování

```bash
# Squash merge zachová čistou lineární historii
# Celý PR = jeden commit v main

# GitHub: "Squash and merge" tlačítko
# Nebo CLI:
git checkout main
git merge --squash feature/order-bulk-update
git commit -m "feat(orders): add bulk status update endpoint"
```

### Proč squash

```
# ✗ Bez squash — zašuměná historie z WIP commitů
abc1234 fix
def5678 WIP
ghi9012 asdf
jkl3456 finally working
mno7890 feat(orders): add bulk update  ← jediný smysluplný commit

# ✓ Se squash — čistá lineární historie
abc1234 feat(orders): add bulk status update endpoint
def5678 fix(auth): prevent session fixation after login
ghi9012 perf(db): add index on Orders.CreatedAt
```

## Tagy a verzování

```bash
# Manuální tag po releasu
git tag -a v1.2.0 -m "Release 1.2.0 — bulk order updates"
git push origin v1.2.0

# Automatické verzování s MinVer (čte git tagy)
# dotnet add package MinVer
# MinVer generuje verzi z tagů: v1.2.0 → 1.2.0
```

## .gitignore — povinné záznamy pro .NET

```gitignore
# Build výstupy
bin/
obj/
*.user
*.suo
.vs/

# Publish
publish/
**/Properties/PublishProfiles/

# Secrets — NIKDY do gitu
**/appsettings.Local.json
**/appsettings.*.Local.json
*.env
.env.*
secrets.json

# Testovací výstupy
TestResults/
coverage/
*.trx

# Logy
logs/
*.log

# OS
.DS_Store
Thumbs.db
```
