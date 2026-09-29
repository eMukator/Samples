# Git — Pull Request & CI/CD Pipeline

## Pull Request — šablona

```markdown
<!-- .github/pull_request_template.md -->
## Co tato změna dělá
<!-- Popis CO a PROČ — ne jak. Odkaz na issue pokud existuje. -->
Closes #123

## Typ změny
- [ ] `feat` — nová funkce
- [ ] `fix` — oprava chyby
- [ ] `perf` — výkon
- [ ] `refactor` — bez funkční změny
- [ ] `chore` — závislosti, konfigurace
- [ ] Breaking change

## Checklist
- [ ] Self-review hotov
- [ ] Testy přidány nebo aktualizovány
- [ ] Žádné nové compiler warnings
- [ ] Žádné secrets v kódu
- [ ] DB migrace přidána (pokud schéma změněno)
- [ ] CLAUDE.md aktualizován (pokud architektura změněna)

## Jak testovat
<!-- Kroky pro reviewera k ověření změny -->
1. ...
2. ...

## Screenshots (pokud UI změna)
```

## GitHub Actions — kompletní CI pipeline

```yaml
# .github/workflows/ci.yml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true  # zruš starý run při novém push

jobs:
  build-test:
    name: Build & Test
    runs-on: ubuntu-latest

    services:
      sqlserver:
        image: mcr.microsoft.com/mssql/server:2022-latest
        env:
          ACCEPT_EULA: "Y"
          SA_PASSWORD: "TestPassword123!"
        ports: ["1433:1433"]
        options: >-
          --health-cmd "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P TestPassword123! -Q 'SELECT 1'"
          --health-interval 10s
          --health-retries 5

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.x'

      - name: Cache NuGet
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: nuget-${{ hashFiles('**/*.csproj') }}

      - name: Restore
        run: dotnet restore

      - name: Build (warnings as errors)
        run: dotnet build --no-restore -warnaserror /p:TreatWarningsAsErrors=true

      - name: Test + Coverage
        run: |
          dotnet test --no-build \
            --collect:"XPlat Code Coverage" \
            --results-directory ./coverage \
            --logger "trx;LogFileName=results.trx" \
            -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
        env:
          ConnectionStrings__Default: "Server=localhost;Database=TestDb;User=sa;Password=TestPassword123!;TrustServerCertificate=true"

      - name: Upload coverage
        uses: codecov/codecov-action@v4
        with:
          directory: ./coverage
          fail_ci_if_error: false

      - name: Publish test results
        uses: dorny/test-reporter@v1
        if: always()
        with:
          name: Test Results
          path: coverage/*.trx
          reporter: dotnet-trx

  security:
    name: Security Scan
    runs-on: ubuntu-latest
    needs: build-test

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.x'

      - name: Check vulnerable packages
        run: |
          dotnet list package --vulnerable --include-transitive 2>&1 \
            | tee vuln-report.txt
          grep -q "has no vulnerable packages" vuln-report.txt || exit 1

      - name: Trivy filesystem scan
        uses: aquasecurity/trivy-action@master
        with:
          scan-type: fs
          severity: CRITICAL,HIGH
          exit-code: 1
          ignore-unfixed: true

  docker:
    name: Build Docker Image
    runs-on: ubuntu-latest
    needs: [build-test, security]
    if: github.ref == 'refs/heads/main'

    steps:
      - uses: actions/checkout@v4

      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v3

      - name: Login to registry
        uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Build and push
        uses: docker/build-push-action@v5
        with:
          context: .
          push: true
          tags: |
            ghcr.io/${{ github.repository }}:latest
            ghcr.io/${{ github.repository }}:${{ github.sha }}
          cache-from: type=gha
          cache-to: type=gha,mode=max
```

## Deploy pipeline — staging → production

```yaml
# .github/workflows/deploy.yml
name: Deploy

on:
  workflow_run:
    workflows: [CI]
    branches: [main]
    types: [completed]

jobs:
  deploy-staging:
    if: ${{ github.event.workflow_run.conclusion == 'success' }}
    runs-on: ubuntu-latest
    environment: staging  # vyžaduje manuální approval v GitHub Environments

    steps:
      - name: Deploy to staging
        run: |
          az webapp deployment container config \
            --name myapp-staging \
            --resource-group myapp-rg \
            --image ghcr.io/${{ github.repository }}:${{ github.sha }}

  deploy-production:
    needs: deploy-staging
    runs-on: ubuntu-latest
    environment: production  # vyžaduje manuální schválení

    steps:
      - name: Deploy to production
        run: |
          az webapp deployment container config \
            --name myapp-prod \
            --resource-group myapp-rg \
            --image ghcr.io/${{ github.repository }}:${{ github.sha }}
```

## Lokální pre-commit hook

```bash
#!/bin/bash
# .git/hooks/pre-commit  (nebo použij Husky)

echo "🔍 Running pre-commit checks..."

# Build
dotnet build --no-incremental -warnaserror -q
if [ $? -ne 0 ]; then
  echo "❌ Build failed"
  exit 1
fi

# Tests (rychlé unit testy)
dotnet test --no-build --filter "Category=Unit" -q
if [ $? -ne 0 ]; then
  echo "❌ Tests failed"
  exit 1
fi

echo "✅ All checks passed"
```
