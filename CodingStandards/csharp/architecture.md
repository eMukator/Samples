# C# — Architektura & Dependency Injection

> Výchozí vrstvená architektura pro **jednoduché CRUD aplikace**. Projekty s business logikou se řídí sadami `ddd/` a `cqrs/`, které mají v případě rozporu přednost (repository per aggregate bez `SaveChanges`, typovaný `Result` s kódem chyby, handlery místo `*Service`). AutoMapper je od 2025 komerční — v nových projektech mapuj ručně nebo přes Mapperly (viz `design-principles/dp-pragmatism.md`).

## Vrstvová struktura (ASP.NET Core)

```
src/
  MyApp.Web/           ← Prezentační vrstva (Controllers, Razor Pages, Views)
  MyApp.Application/   ← Business logika (Services, Use cases, DTOs)
  MyApp.Domain/        ← Doménové modely, Value objects, Enums
  MyApp.Infrastructure/← Data access (EF Core, repozitáře, externí API)
tests/
  MyApp.UnitTests/
  MyApp.IntegrationTests/
```

### Závislosti smí jít pouze jedním směrem
```
Web → Application → Domain
Infrastructure → Application (implementuje interfaces z Application)
```

- **Domain** nezná nic o EF Core, HTTP, ani DI
- **Application** nezná nic o konkrétní DB ani HTTP
- **Web** pouze orchestruje — žádná business logika v Controllerech

## Controllers / Razor Pages

```csharp
// ✓ Správně — slim controller, deleguje na service
[ApiController]
[Route("api/[controller]")]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetById(
        int id,
        CancellationToken ct)
    {
        var order = await orderService.GetByIdAsync(id, ct);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(
        CreateOrderRequest request,
        CancellationToken ct)
    {
        var result = await orderService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}

// ✗ Špatně — business logika v controlleru
[HttpPost]
public async Task<ActionResult> Create(CreateOrderRequest request)
{
    if (request.Items.Sum(i => i.Price) > 10000)  // business pravidlo patří do service
        return BadRequest("...");

    var order = new Order { ... };    // mapování patří do service/mapper
    _context.Orders.Add(order);
    await _context.SaveChangesAsync();
    return Ok(order);
}
```

## Services

```csharp
// Interface vždy v Application vrstvě
public interface IOrderService
{
    Task<OrderDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken ct = default);
    Task<PagedResult<OrderDto>> GetPagedAsync(OrderFilter filter, CancellationToken ct = default);
}

// Implementace v Application nebo Infrastructure
public class OrderService(
    IOrderRepository repository,
    IMapper mapper,
    ILogger<OrderService> logger) : IOrderService
{
    public async Task<OrderDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        logger.LogDebug("Fetching order {OrderId}", id);
        var order = await repository.GetByIdAsync(id, ct);
        return order is null ? null : mapper.Map<OrderDto>(order);
    }
}
```

## Repository pattern

```csharp
// Interface
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetByCustomerAsync(int customerId, CancellationToken ct = default);
    Task<Order> AddAsync(Order order, CancellationToken ct = default);
    Task UpdateAsync(Order order, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

// Implementace s EF Core
public class OrderRepository(AppDbContext context) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(int id, CancellationToken ct = default)
        => await context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<Order> AddAsync(Order order, CancellationToken ct = default)
    {
        context.Orders.Add(order);
        await context.SaveChangesAsync(ct);
        return order;
    }
}
```

## Dependency Injection — registrace

```csharp
// Program.cs — organizuj registrace do extension metod
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// ApplicationServiceExtensions.cs
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddAutoMapper(typeof(MappingProfile));
        return services;
    }
}

// InfrastructureServiceExtensions.cs
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlServer(config.GetConnectionString("Default")));

        services.AddScoped<IOrderRepository, OrderRepository>();
        return services;
    }
}
```

## Lifetimes

| Typ | Kdy použít |
|-----|-----------|
| `Scoped` | Services, Repositories (default pro web request) |
| `Singleton` | Cache, statická konfigurace, thread-safe utility |
| `Transient` | Lehké, stavové objekty (helpers, validators) |

```csharp
// ✗ Captive dependency — nikdy neinjektuj Scoped do Singleton!
public class MySingletonService(IScopedRepository repo) { }  // CHYBA
```

## Result pattern (místo výjimek pro business chyby)

```csharp
public record Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }

    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error };
}

// Použití v service
public async Task<Result<OrderDto>> CreateAsync(CreateOrderRequest request, CancellationToken ct)
{
    if (!request.IsValid)
        return Result<OrderDto>.Failure("Neplatná objednávka");

    var order = await _repository.CreateAsync(MapToOrder(request), ct);
    return Result<OrderDto>.Success(MapToDto(order));
}

// V controlleru
var result = await _orderService.CreateAsync(request, ct);
return result.IsSuccess
    ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
    : BadRequest(result.Error);
```
