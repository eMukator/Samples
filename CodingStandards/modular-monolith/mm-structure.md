# Modulární monolit — struktura

## Projekty

```
src/
  Host/MyApp.Host/                     ← Program.cs, composition root, jediný spustitelný projekt
  Shared/MyApp.Shared/                 ← jen technické minimum: Result, IDomainEvent, outbox abstrakce
  Modules/
    Ordering/
      MyApp.Ordering.Contracts/        ← PUBLIC: integration events, rozhraní pro sync dotazy, jejich DTO
      MyApp.Ordering/                  ← INTERNAL: Domain + Application + Infrastructure modulu
    Catalog/
      MyApp.Catalog.Contracts/
      MyApp.Catalog/
tests/
  MyApp.Ordering.Tests/
  MyApp.ArchitectureTests/
```

- Malý modul = jeden implementační projekt se složkami `Domain/`, `Features/`, `Infrastructure/`. Velký modul smí mít vlastní Clean rozdělení na projekty.
- `MyApp.Shared` drž **malý** — nesmí se z něj stát odkladiště business logiky (sdílené entity, enumy domény).
- ✗ Sdílená entita `Product` používaná více moduly. Každý modul má vlastní model (viz bounded context).

---

## Registrace modulu

```csharp
// MyApp.Ordering/OrderingModule.cs — jediný public typ implementačního projektu
public static class OrderingModule
{
    public static IServiceCollection AddOrderingModule(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<OrderingDbContext>(o =>
            o.UseSqlServer(config.GetConnectionString("Default"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", OrderingDbContext.Schema)));

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderingApi, OrderingApi>();          // implementace kontraktu pro ostatní moduly
        return services;
    }

    public static IEndpointRouteBuilder MapOrderingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/orders").WithTags("Ordering");
        group.MapConfirmOrder();
        group.MapGetOrderDetail();
        return app;
    }
}

// Program.cs
builder.Services
    .AddOrderingModule(builder.Configuration)
    .AddCatalogModule(builder.Configuration);

app.MapOrderingEndpoints();
app.MapCatalogEndpoints();
```

✗ Reflexní auto-discovery modulů (`IModule` + scan assembly) — explicitní seznam v `Program.cs` je čitelnější.

---

## Databáze — schéma na modul

```csharp
internal sealed class OrderingDbContext(DbContextOptions<OrderingDbContext> options) : DbContext(options)
{
    public const string Schema = "ordering";

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.HasDefaultSchema(Schema);
        mb.ApplyConfigurationsFromAssembly(typeof(OrderingDbContext).Assembly,
            t => t.Namespace?.StartsWith("MyApp.Ordering") == true);
    }
}
```

```bash
# Migrace per modul
dotnet ef migrations add AddOrderNote --project src/Modules/Ordering/MyApp.Ordering --startup-project src/Host/MyApp.Host --context OrderingDbContext
```

- Jedna fyzická DB je OK — oddělení zajišťuje schéma. Pro silnější izolaci: DB uživatel per modul s právy jen na své schéma.
- ✗ Cizí klíč `ordering.Orders.CustomerId → customers.Customers.Id`. Modul drží jen ID (a případně lokální kopii potřebných dat).
- ✗ Join přes schémata v dotazu. Potřebná data získej přes kontrakt nebo replikuj událostmi (viz `mm-communication.md`).
- Outbox / Inbox tabulky má každý modul ve svém schématu (viz `event-driven/ed-outbox-inbox.md`).

---

## Hlídání hranic

Hlavní kontrola je **kompilátor**: implementační projekt modulu smí mít `ProjectReference` jen na `MyApp.Shared` a na `*.Contracts` ostatních modulů. Test hlídá, aby nikdo zakázanou referenci nepřidal.

```csharp
public sealed class ModuleBoundaryTests
{
    private static readonly string ModulesDir = Path.Combine(FindSolutionRoot(), "src", "Modules");

    [Fact]
    public void ModuleProjects_ReferenceOtherModulesOnlyThroughContracts()
    {
        var offenders =
            from csproj in Directory.GetFiles(ModulesDir, "*.csproj", SearchOption.AllDirectories)
            let ownModule = ModuleOf(csproj)
            from reference in XDocument.Load(csproj).Descendants("ProjectReference")
            let target = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(csproj)!, reference.Attribute("Include")!.Value))
            where target.StartsWith(ModulesDir, StringComparison.OrdinalIgnoreCase)
            where ModuleOf(target) != ownModule
            where !target.EndsWith(".Contracts.csproj", StringComparison.OrdinalIgnoreCase)
            select $"{Path.GetFileName(csproj)} → {Path.GetFileName(target)}";

        Assert.Empty(offenders);
    }

    [Fact]
    public void Contracts_DoNotReferenceImplementations()
    {
        var offenders =
            from csproj in Directory.GetFiles(ModulesDir, "*.Contracts.csproj", SearchOption.AllDirectories)
            from reference in XDocument.Load(csproj).Descendants("ProjectReference")
            let include = reference.Attribute("Include")!.Value
            where !include.EndsWith(".Contracts.csproj", StringComparison.OrdinalIgnoreCase)
               && !include.Contains("MyApp.Shared", StringComparison.OrdinalIgnoreCase)
            select $"{Path.GetFileName(csproj)} → {include}";

        Assert.Empty(offenders);
    }

    // src/Modules/{Module}/... → {Module}
    private static string ModuleOf(string path)
        => Path.GetRelativePath(ModulesDir, path).Split(Path.DirectorySeparatorChar)[0];

    private static string FindSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && dir.GetFiles("*.sln*").Length == 0) dir = dir.Parent;   // .sln i .slnx
        return dir?.FullName ?? throw new InvalidOperationException("Solution root nenalezen.");
    }
}
```

Doplňkově NetArchTest pro pravidla uvnitř modulu (Domain bez EF Core apod. — viz `clean-architecture/ca-enforcement.md`).

