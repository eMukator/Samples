# C# — Async/Await

## Základní pravidla

- Každá I/O operace musí být `async` — nikdy neblokuj vlákno synchronně
- Async metody mají vždy suffix `Async`: `GetOrderAsync`, `SaveChangesAsync`
- `CancellationToken` parametr na všech async veřejných metodách (výchozí hodnota `= default`)
- Nikdy `async void` — výjimka: event handlery a lifecycle metody (Blazor `OnInitializedAsync`)

## Správné vzory

```csharp
// ✓ Správně — async všude, CancellationToken propagován
public async Task<Order?> GetByIdAsync(int id, CancellationToken ct = default)
{
    return await _context.Orders
        .AsNoTracking()
        .FirstOrDefaultAsync(o => o.Id == id, ct);
}

// ✓ Správně — ConfigureAwait(false) v knihovnách (ne v ASP.NET Core app kódu)
// V ASP.NET Core není ConfigureAwait(false) nutný (žádný SynchronizationContext)
public async Task<string> FetchDataAsync(string url, CancellationToken ct = default)
{
    return await _httpClient.GetStringAsync(url, ct);
}

// ✓ Správně — ValueTask pro horké cesty kde je výsledek often synchronní
public ValueTask<Order?> GetCachedOrderAsync(int id)
{
    if (_cache.TryGetValue(id, out Order? order))
        return ValueTask.FromResult(order);

    return new ValueTask<Order?>(FetchOrderAsync(id));
}
```

## Zakázané vzory

```csharp
// ✗ Nikdy — blokování async kódu → deadlock
var order = GetOrderAsync(id).Result;
var order = GetOrderAsync(id).GetAwaiter().GetResult();
Task.Run(() => GetOrderAsync(id)).Wait();

// ✗ Nikdy async void (výjimky jsou nezachytitelné)
public async void LoadData()  // CHYBA
{
    var data = await _service.GetDataAsync();
}

// ✓ Správně
public async Task LoadDataAsync()
{
    var data = await _service.GetDataAsync();
}

// ✗ Nikdy — zapomenutý await (fire and forget bez ošetření)
_emailService.SendEmailAsync(email);  // chyba je spolknuta

// ✓ Pokud opravdu potřebuješ fire-and-forget, ošetři chyby
_ = _emailService.SendEmailAsync(email)
    .ContinueWith(t => _logger.LogError(t.Exception, "Email failed"),
                  TaskContinuationOptions.OnlyOnFaulted);
```

## Paralelní operace

```csharp
// ✓ Paralelní nezávislé operace — WhenAll
var (orders, customers) = await (
    _orderService.GetAllAsync(ct),
    _customerService.GetAllAsync(ct)
).WhenAll();  // C# 7+ tuple deconstruct

// Nebo explicitně
var ordersTask = _orderService.GetAllAsync(ct);
var customersTask = _customerService.GetAllAsync(ct);
await Task.WhenAll(ordersTask, customersTask);
var orders = await ordersTask;
var customers = await customersTask;

// ✗ Špatně — sekvenční volání kde jsou nezávislé
var orders = await _orderService.GetAllAsync(ct);       // zbytečné čekání
var customers = await _customerService.GetAllAsync(ct);

// ✓ Batch processing s omezením paralelizace
var semaphore = new SemaphoreSlim(10);  // max 10 paralelně
var tasks = items.Select(async item =>
{
    await semaphore.WaitAsync(ct);
    try { return await ProcessItemAsync(item, ct); }
    finally { semaphore.Release(); }
});
var results = await Task.WhenAll(tasks);
```

## CancellationToken

```csharp
// ✓ Vždy přijmi a propaguj CancellationToken
public async Task<IReadOnlyList<OrderDto>> GetFilteredAsync(
    OrderFilter filter,
    CancellationToken ct = default)
{
    var orders = await _repository.GetByFilterAsync(filter, ct);
    return orders.Select(o => MapToDto(o)).ToList();
}

// ✓ Kombinování tokenů (timeout + user cancel)
using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
cts.CancelAfter(TimeSpan.FromSeconds(30));

try
{
    var result = await _externalApi.CallAsync(cts.Token);
}
catch (OperationCanceledException) when (!ct.IsCancellationRequested)
{
    throw new TimeoutException("Požadavek vypršel po 30 sekundách");
}

// ✓ V ASP.NET Core — HttpContext.RequestAborted
public async Task<IActionResult> GetOrders(CancellationToken ct)
{
    // ct je automaticky HttpContext.RequestAborted
    var orders = await _service.GetAllAsync(ct);
    return Ok(orders);
}
```

## Async v konstruktoru — vzor factory

```csharp
// ✗ Konstruktor nemůže být async
public class ReportService
{
    public ReportService()
    {
        _data = await LoadDataAsync();  // NELZE
    }
}

// ✓ Factory pattern
public class ReportService
{
    private readonly IReadOnlyList<ReportData> _data;

    private ReportService(IReadOnlyList<ReportData> data) => _data = data;

    public static async Task<ReportService> CreateAsync(CancellationToken ct = default)
    {
        var data = await LoadDataAsync(ct);
        return new ReportService(data);
    }
}

// Registrace v DI přes IHostedService nebo Lazy initialization
```
