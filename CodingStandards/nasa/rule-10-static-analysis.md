# Pravidlo 10 — Statická analýza

> *"Kód musí projít alespoň dvěma různými statickými analyzátory. Žádný nástroj nezachytí vše sám."*

## Doporučená kombinace nástrojů

| Nástroj | Co zachytí | Jak přidat |
|---------|-----------|-----------|
| **Roslyn Analyzers** (vestavěné) | Nullable, code style, základní chyby | `<AnalysisMode>All</AnalysisMode>` v csproj |
| **SonarAnalyzer.CSharp** | Security, code smells, duplicity | NuGet balíček |
| **Roslynator** | 500+ code quality pravidel | NuGet balíček |
| **SonarQube / SonarCloud** | Deep security scan, coverage | CI integrace |
| **Semgrep** | Custom security pravidla | CI pipeline |

## Minimální setup — NuGet analyzátory

```xml
<!-- *.csproj — přidej do každého projektu -->
<ItemGroup>
  <PackageReference Include="SonarAnalyzer.CSharp"
                    Version="*"
                    PrivateAssets="all"
                    IncludeAssets="runtime; build; native; contentfiles; analyzers" />

  <PackageReference Include="Roslynator.Analyzers"
                    Version="*"
                    PrivateAssets="all"
                    IncludeAssets="runtime; build; native; contentfiles; analyzers" />

  <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers"
                    Version="*"
                    PrivateAssets="all"
                    IncludeAssets="runtime; build; native; contentfiles; analyzers" />
</ItemGroup>
```

## Konfigurace pravidel

```ini
# .editorconfig — příklady klíčových pravidel

# Security
dotnet_diagnostic.S2077.severity = error   # SQL injection
dotnet_diagnostic.S2076.severity = error   # OS command injection
dotnet_diagnostic.S5144.severity = error   # path traversal
dotnet_diagnostic.S2068.severity = error   # hardcoded credentials
dotnet_diagnostic.S2245.severity = warning # weak random (použij CryptoRandom pro security)

# Reliability
dotnet_diagnostic.S3966.severity = error   # dispose called twice
dotnet_diagnostic.S2583.severity = error   # conditions always true/false
dotnet_diagnostic.S1764.severity = error   # identical expressions on both sides

# Code smells
dotnet_diagnostic.S1135.severity = warning # TODO komentáře (sleduj, nevypínej)
dotnet_diagnostic.S3776.severity = warning # cognitive complexity > 15
dotnet_diagnostic.S1066.severity = warning # mergeable if statements
```

## SonarQube v CI (GitHub Actions)

```yaml
# .github/workflows/sonar.yml
name: SonarQube Analysis

on: [push, pull_request]

jobs:
  sonar:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.x'

      - name: Install SonarScanner
        run: dotnet tool install --global dotnet-sonarscanner

      - name: Begin analysis
        run: |
          dotnet sonarscanner begin \
            /k:"my-project-key" \
            /d:sonar.login="${{ secrets.SONAR_TOKEN }}" \
            /d:sonar.cs.opencover.reportsPaths="coverage.xml"

      - name: Build
        run: dotnet build --no-incremental

      - name: Test with coverage
        run: dotnet test --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

      - name: End analysis
        run: dotnet sonarscanner end /d:sonar.login="${{ secrets.SONAR_TOKEN }}"
```

## Lokální analýza — před každým commitem

```bash
# Přidej do pre-commit hook nebo lokálního skriptu
#!/bin/bash

echo "Running static analysis..."

# Build s warnings jako errors
dotnet build -warnaserror || exit 1

# Testy
dotnet test --no-build || exit 1

# Security scan (pokud máš Semgrep)
# semgrep --config=p/csharp --error || exit 1

echo "All checks passed ✓"
```

## Minimální quality gate (doporučené prahové hodnoty)

| Metrika | Minimum |
|---------|---------|
| Test coverage nového kódu | ≥ 80 % |
| Duplicitní kód | < 3 % |
| Kritické bugy | 0 |
| Security vulnerabilities | 0 |
| Security hotspots reviewovány | 100 % |
| Cognitive complexity per method | ≤ 15 |

## Poznámka ke třetím stranám

Statická analýza generovaného kódu (migrations, scaffolding) lze vypnout:

```xml
<!-- pro EF Core migrace -->
<ItemGroup>
  <Compile Remove="Migrations\**" />
</ItemGroup>

<!-- nebo v .editorconfig -->
[Migrations/**.cs]
dotnet_analyzer_diagnostic.severity = none
generated_code = true
```
