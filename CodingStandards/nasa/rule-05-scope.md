# Pravidlo 5 — Minimální scope dat

> *"Data musí mít co nejmenší scope. Sdílený globální stav je zdroj chyb."*

## C# adaptace

### Proměnné — deklaruj co nejblíže použití

```csharp
// ✗ Deklarace daleko od použití (C styl)
public async Task ProcessOrdersAsync(CancellationToken ct)
{
    List<Order> orders;
    decimal total;
    string customerName;
    bool isValid;

    orders = await _repository.GetPendingAsync(ct);
    // ... 20 řádků jiného kódu ...
    total = orders.Sum(o => o.Total);  // kde se total použije poprvé?
}

// ✓ Deklarace těsně před prvním použitím
public async Task ProcessOrdersAsync(CancellationToken ct)
{
    var orders = await _repository.GetPendingAsync(ct);

    foreach (var order in orders)
    {
        var total = CalculateTotal(order);  // total žije jen v tomto bloku
        await ProcessSingleOrderAsync(order, total, ct);
    }
}
```

### Třídy — minimalizuj stav

```csharp
// ✗ Zbytečný sdílený stav v třídě
public class OrderProcessor
{
    private Order _currentOrder;     // sdílený stav — race condition v async!
    private decimal _calculatedTotal;
    private bool _isProcessing;

    public async Task ProcessAsync(int orderId, CancellationToken ct)
    {
        _currentOrder = await _repo.GetAsync(orderId, ct);
        _calculatedTotal = Calculate(_currentOrder);
        _isProcessing = true;
        // ...
    }
}

// ✓ Stav předávaný jako parametr — bezpečné pro async, testovatelné
public class OrderProcessor
{
    public async Task ProcessAsync(int orderId, CancellationToken ct)
    {
        var order = await _repo.GetAsync(orderId, ct);
        var total = Calculate(order);
        await SaveAsync(order, total, ct);
    }

    private decimal Calculate(Order order) => /* čistá funkce, žádný sdílený stav */;
}
```

### Viditelnost — vždy co nejrestriktivnější

```csharp
// ✓ Postup: private → private readonly → internal → protected → public
public class OrderService
{
    private readonly IOrderRepository _repository;  // readonly — nikdy nepřepsáno
    private readonly ILogger<OrderService> _logger;

    // ✓ Pomocné metody jsou private
    private Order MapToEntity(CreateOrderRequest request) { ... }
    private OrderDto MapToDto(Order order) { ... }

    // ✓ Veřejné jen to, co je skutečně rozhraní
    public async Task<OrderDto?> GetByIdAsync(int id, CancellationToken ct) { ... }
    public async Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken ct) { ... }
}

// ✓ sealed zabraňuje nechtěnému dědění (výchozí pro services)
public sealed class OrderService : IOrderService { }
```

### Statický stav — zakázán bez synchronizace

```csharp
// ✗ Mutable static state — race condition
public class OrderCache
{
    private static List<Order> _cache = new();  // CHYBA — sdíleno napříč requesty

    public static void Add(Order order) => _cache.Add(order);  // není thread-safe
}

// ✓ IMemoryCache nebo ConcurrentDictionary přes DI (Singleton se správnou synchronizací)
public class OrderCache(IMemoryCache cache)
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    public void Set(int id, Order order)
        => cache.Set($"order:{id}", order, Ttl);

    public Order? Get(int id)
        => cache.TryGetValue($"order:{id}", out Order? order) ? order : null;
}

// ✓ Thread-local state kde je opravdu potřeba
[ThreadStatic]
private static int _requestDepth;
```

### Records a immutability jako výchozí

```csharp
// ✓ Preferuj immutable datové struktury — minimalizují chyby ze sdíleného stavu
public record OrderDto(
    int Id,
    string CustomerName,
    decimal Total,
    OrderStatus Status,
    IReadOnlyList<OrderItemDto> Items);

// ✓ with expression pro "změnu" immutable recordu
var updated = original with { Status = OrderStatus.Shipped };
// original je nezměněn
```
