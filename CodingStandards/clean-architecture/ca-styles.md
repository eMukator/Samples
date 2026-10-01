# Architektonické styly — výběr

## Rozhodovací tabulka

| Situace | Styl |
|---------|------|
| Prototyp, malý nástroj, < ~20 obrazovek/endpointů | **Jeden projekt**, složky podle feature |
| CRUD aplikace bez výrazné logiky | **Vrstvená** (`csharp/architecture.md`) |
| Netriviální doména, jeden tým | **Clean / Onion** + vertical slices v Application |
| Rostoucí aplikace, více oblastí domény / týmů | **Modulární monolit** (viz `modular-monolith/`), uvnitř modulů Clean |
| Nezávislé nasazování, rozdílné škálování, samostatné týmy | Mikroslužby — až po modulárním monolitu, ne jako start |

✗ Nezačínej mikroslužbami. Špatně zvolené hranice v monolitu se přesouvají refaktoringem, v mikroslužbách migrací dat.

---

## Clean / Onion — struktura

```
src/
  MyApp.Domain/            ← entity, value objects, domain events, doménové služby; ŽÁDNÉ NuGety (kromě případně analyzérů)
  MyApp.Application/       ← use cases (handlery), porty (IOrderRepository, IEmailSender, IClock), DTO
  MyApp.Infrastructure/    ← EF Core, HTTP klienti, e-mail, broker — implementace portů
  MyApp.Web/               ← endpointy / Blazor, composition root (Program.cs)
tests/
  MyApp.Domain.Tests/
  MyApp.Application.Tests/
  MyApp.IntegrationTests/
  MyApp.ArchitectureTests/
```

### Pravidlo závislostí

```
Web ──────────────► Application ──► Domain
 │                       ▲
 └──► Infrastructure ────┘
```

- Web referencuje Infrastructure **jen kvůli registraci DI** (composition root).
- Application **nezná** EF Core. Výjimka, kterou je potřeba vědomě rozhodnout (ADR): query handlery v Application používají přímo `DbContext` (pragmatický CQRS) — pak Application referencuje EF Core, ale Domain stále ne.

| Varianta query side | Plus | Mínus |
|---------------------|------|-------|
| `IAppDbContext` interface v Application s `DbSet<T>` | Jednoduché, LINQ projekce | Application závisí na EF Core |
| Query handlery v Infrastructure | Čistá Application | Use case rozdělen do dvou projektů |
| Dapper / SQL v Infrastructure za `IOrderQueries` | Výkon, plná kontrola | Ruční SQL, víc kódu |

Doporučení: první varianta, zapsaná jako ADR.

---

## Porty a adaptéry

```csharp
// Application — port pojmenovaný podle potřeby use case, ne podle technologie
public interface IPaymentGateway
{
    Task<PaymentResult> ChargeAsync(Money amount, PaymentMethodToken token, CancellationToken ct);
}

// Infrastructure — adaptér
internal sealed class StripePaymentGateway(StripeClient client) : IPaymentGateway { ... }

// ✗ Port kopírující API dodavatele
public interface IStripeService { Task<StripeCharge> CreateChargeAsync(StripeChargeOptions options); }
```

---

## Vertical slices uvnitř Clean

Clean určuje **směr závislostí mezi projekty**, vertical slices **organizaci uvnitř Application**. Kombinuj (viz `cqrs/cqrs-vertical-slices.md`):

```
MyApp.Application/
  Orders/
    ConfirmOrder/
    GetOrderDetail/
  Customers/
    RegisterCustomer/
  Abstractions/          ← porty sdílené více features
```

✗ Složky `Services/`, `Interfaces/`, `Dtos/`, `Validators/` v Application.

---

## Composition root

```csharp
// Program.cs — jediné místo, které zná všechny vrstvy
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

// Každý projekt vystavuje jednu extension metodu; implementace jsou internal
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(o => o.UseSqlServer(config.GetConnectionString("Default")));
        services.AddScoped<IOrderRepository, OrderRepository>();     // OrderRepository je internal
        services.AddScoped<IPaymentGateway, StripePaymentGateway>();
        return services;
    }
}
```
