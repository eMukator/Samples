# Vynucení architektury

Pravidlo, které nehlídá build nebo test, se dřív nebo později poruší — AI agentem dvojnásob.

## 1. Reference mezi projekty (kompilátor)

Nejlevnější kontrola: projekt, který referenci nemá, nemůže závislost porušit.

```xml
<!-- MyApp.Domain.csproj — žádné ProjectReference ani PackageReference na infrastrukturu -->

<!-- MyApp.Application.csproj -->
<ItemGroup>
  <ProjectReference Include="..\MyApp.Domain\MyApp.Domain.csproj" />
</ItemGroup>
```

✗ Nepřidávej referenci „protože to bylo rychlejší" — přesuň typ do správné vrstvy nebo zaveď port.

---

## 2. `internal` jako výchozí

```csharp
// Infrastructure — implementace nejsou vidět z Web projektu, jde se jen přes porty
internal sealed class OrderRepository(AppDbContext db) : IOrderRepository { ... }
```

```xml
<!-- Přístup pro testy -->
<ItemGroup>
  <InternalsVisibleTo Include="MyApp.Infrastructure.Tests" />
</ItemGroup>
```

✗ `InternalsVisibleTo` pro produkční projekty — obchází hranici.

---

## 3. Architektonické testy (NetArchTest.Rules)

```csharp
public sealed class LayerTests
{
    private static readonly Assembly Domain = typeof(Order).Assembly;
    private static readonly Assembly Application = typeof(ConfirmOrderHandler).Assembly;
    private static readonly Assembly Infrastructure = typeof(AppDbContext).Assembly;

    [Fact]
    public void Domain_HasNoInfrastructureDependencies()
    {
        var result = Types.InAssembly(Domain)
            .ShouldNot().HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "System.Net.Http",
                Application.GetName().Name!,
                Infrastructure.GetName().Name!)
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Application_DoesNotDependOnInfrastructure()
    {
        var result = Types.InAssembly(Application)
            .ShouldNot().HaveDependencyOn(Infrastructure.GetName().Name!)
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Handlers_AreSealedAndInternal()
    {
        var result = Types.InAssembly(Application)
            .That().HaveNameEndingWith("Handler")
            .Should().BeSealed().And().NotBePublic()
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void DomainEntities_HaveNoPublicSetters()
    {
        var offenders = Domain.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.SetMethod?.IsPublic == true && !p.SetMethod.ReturnParameter
                    .GetRequiredCustomModifiers().Contains(typeof(System.Runtime.CompilerServices.IsExternalInit)))
                .Select(p => $"{t.Name}.{p.Name}"))
            .ToList();

        Assert.Empty(offenders);
    }

    private static string Describe(TestResult r)
        => "Porušení: " + string.Join(", ", r.FailingTypeNames ?? []);
}
```

- Testy patří do samostatného projektu `*.ArchitectureTests` a běží v CI pipeline (viz `git-cicd/`).
- Hláška musí říct **který typ** pravidlo porušil — jinak test nikdo neopraví.
- Alternativa: ArchUnitNET (bohatší API, pravidla nad celým modelem).

---

## 4. Analyzéry a build

```xml
<!-- Directory.Build.props v kořeni repozitáře -->
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
</Project>
```

```ini
# .editorconfig — příklad pravidel podporujících architekturu
dotnet_diagnostic.CA1852.severity = warning   # sealed pro typy bez potomků
dotnet_diagnostic.CA1515.severity = warning   # internal pro typy v aplikačních projektech (.NET 9+)
```

Viz též `nasa/rule-09-warnings.md` a `nasa/rule-10-static-analysis.md`.
