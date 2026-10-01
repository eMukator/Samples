# C# — Naming Conventions

## Přehled konvencí

| Typ | Konvence | Příklad |
|-----|----------|---------|
| Třída, record, struct | PascalCase | `OrderService`, `UserDto` |
| Interface | `I` + PascalCase | `IOrderRepository` |
| Enum | PascalCase | `OrderStatus` |
| Enum hodnota | PascalCase | `OrderStatus.Pending` |
| Veřejná property | PascalCase | `FirstName` |
| Veřejná metoda | PascalCase | `GetOrderAsync` |
| Privátní field (instanční i mutable static) | `_` + camelCase | `_orderRepository`, `_activeCount` |
| Konstanta (`const`) | PascalCase | `MaxRetryCount` |
| `static readonly` field (jakákoli přístupnost) | PascalCase | `DefaultTimeout`, `HandlerCache` |
| Lokální proměnná | camelCase | `orderTotal` |
| Parametr | camelCase | `orderId` |
| Generic type param | `T` nebo `T` + PascalCase | `T`, `TEntity` |
| Async metoda | PascalCase + `Async` suffix | `GetOrderAsync` |

## Třídy a soubory

- Jeden soubor = jedna veřejná třída/record/interface
- Název souboru = název třídy: `OrderService.cs` obsahuje `OrderService`
- Namespace odpovídá adresářové struktuře projektu

```csharp
// ✓ Správně
namespace MyApp.Orders.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _repository;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IOrderRepository repository, ILogger<OrderService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
}
```

## Interfaces

```csharp
// ✓ Správně
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default);
    Task<Order> CreateAsync(Order order, CancellationToken ct = default);
}

// ✗ Špatně — chybí I prefix
public interface OrderRepository { }
```

## Records a DTOs

```csharp
// ✓ Správně — immutable record pro DTOs
public record OrderDto(int Id, string CustomerName, decimal Total, OrderStatus Status);

// ✓ Správně — record s validací
public record CreateOrderRequest(
    string CustomerName,
    IReadOnlyList<OrderLineItem> Items)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(CustomerName) && Items.Count > 0;
}

// ✗ Špatně — mutable class pro DTO bez důvodu
public class OrderDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; }
}
```

## Enums

```csharp
// ✓ Správně
public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}

// ✗ Špatně — lowercase hodnoty, magic number v kódu
if (order.Status == 2) { }   // co je 2?
if (order.Status == OrderStatus.Shipped) { }  // ✓
```

## Konstanty a magic values

```csharp
// ✗ Špatně
if (retryCount > 3) { }
var url = "https://api.example.com/v1";

// ✓ Správně
private const int MaxRetryCount = 3;
private const string ApiBaseUrl = "https://api.example.com/v1";

// ✓ Správně — statická readonly pro komplexní hodnoty (PascalCase jako konstanta)
private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
```

Pravidlo se řídí **modifikátory**, ne obsahem: `const` a `static readonly` → PascalCase, vše ostatní privátní → `_camelCase`. Díky tomu ho vynutí `.editorconfig` (viz `code-style.md`).

```csharp
// ✓ Mutable static — _camelCase, ale vyhýbej se mu (sdílený stav, viz nasa/rule-05-scope.md)
private static int _activeCount;   // jen s Interlocked / lock
```

## Generics

```csharp
// ✓ Správně — T pro jednoduché, popisné pro složité
public interface IQueryHandler<in TQuery, TResult> { }
public class Result<TValue, TError> { }

// ✗ Špatně — nečitelné zkratky
public interface IHandler<Q, R> { }
```
