# Pravidlo 9 — Žádné compiler warnings

> *"Kód musí být kompilovatelný bez warningů na nejvyšší úrovni varování. Warnings jsou chyby čekající na příležitost."*

## C# nastavení projektu

```xml
<!-- *.csproj -->
<PropertyGroup>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <WarningLevel>9999</WarningLevel>       <!-- nejvyšší úroveň warningů -->
  <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  <AnalysisMode>All</AnalysisMode>        <!-- všechny Roslyn analyzátory -->
  <CodeAnalysisTreatWarningsAsErrors>true</CodeAnalysisTreatWarningsAsErrors>
</PropertyGroup>
```

### Povolené výjimky (suppress s komentářem)

Pokud warning skutečně nelze odstranit (třetí strana, legacy kód, záměrné rozhodnutí):

```csharp
// ✓ Suppress s explicitním zdůvodněním — nikdy bez komentáře
#pragma warning disable CS8618  // Non-nullable field — inicializováno v OnModelCreating
public DbSet<Order> Orders { get; set; }
#pragma warning restore CS8618

// nebo přes atribut
[SuppressMessage("Reliability", "CA2007:ConfigureAwait",
    Justification = "ASP.NET Core nepotřebuje ConfigureAwait(false)")]
public async Task<Order> GetAsync(int id) { }
```

### Nejčastější warnings a jejich řešení

**CS8618 — Non-nullable field not initialized**
```csharp
// ✗ Problem
public class OrderViewModel
{
    public string CustomerName { get; set; }  // warning: neinicializováno
}

// ✓ Řešení 1 — inicializuj
public string CustomerName { get; set; } = string.Empty;

// ✓ Řešení 2 — nullable pokud skutečně může být null
public string? CustomerName { get; set; }

// ✓ Řešení 3 — required (C# 11+)
public required string CustomerName { get; init; }
```

**CS8600/CS8602 — Possible null dereference**
```csharp
// ✗ Problem
Order? order = await _repo.GetByIdAsync(id);
var total = order.Total;  // warning: order může být null

// ✓ Řešení — null check
var order = await _repo.GetByIdAsync(id)
    ?? throw new OrderNotFoundException(id);
var total = order.Total;
```

**CA1848 — Use LoggerMessage.Define**
```csharp
// ✗ Problem (warning v produkčním kódu)
_logger.LogInformation($"Processing order {orderId}");

// ✓ Řešení — structured logging
_logger.LogInformation("Processing order {OrderId}", orderId);
```

**CA2007 — ConfigureAwait**
```csharp
// V ASP.NET Core aplikacích suppress globálně v .editorconfig:
// dotnet_diagnostic.CA2007.severity = none
```

### .editorconfig — nastavení závažnosti

```ini
[*.cs]
# Errors — musí být opraveny
dotnet_diagnostic.CS8618.severity = error  # nullable
dotnet_diagnostic.CS8602.severity = error  # null dereference
dotnet_diagnostic.CA2000.severity = error  # IDisposable not disposed

# Warnings — zobrazí se, ale neblokují build (pro postupnou migraci)
dotnet_diagnostic.CA1031.severity = warning  # catch generic exception
dotnet_diagnostic.CA1307.severity = warning  # StringComparison

# Disabled — záměrně vypnuto
dotnet_diagnostic.CA2007.severity = none  # ConfigureAwait — ASP.NET Core
dotnet_diagnostic.CA1062.severity = none  # validate parameter — pokrýváme guard clauses
```

### CI pipeline — build musí selhat při warningu

```yaml
# GitHub Actions
- name: Build
  run: dotnet build --no-restore -warnaserror /p:TreatWarningsAsErrors=true

- name: Test
  run: dotnet test --no-build --verbosity normal
```
