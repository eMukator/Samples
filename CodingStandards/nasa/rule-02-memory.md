# Pravidlo 2 — Žádná nekontrolovaná alokace paměti

> *"Po inicializaci nepoužívej dynamickou alokaci paměti."*

## C# adaptace

GC paměť spravuje automaticky, ale to neznamená, že na alokacích nezáleží. Duch pravidla v C# znamená:
- nealokovat nekontrolovaně ve smyčkách a hot paths
- vždy uvolňovat `IDisposable` zdroje (`using`)
- nikdy nenechávat zdroje (DB spojení, soubory, HTTP klienty) bez explicitního uvolnění

### Pravidla pro IDisposable

```csharp
// ✗ Nikdy — zdroj není uvolněn při výjimce
var connection = new SqlConnection(connectionString);
connection.Open();
DoWork(connection);
connection.Dispose();  // nikdy nedosaženo při výjimce

// ✓ using statement — uvolnění garantováno vždy
using var connection = new SqlConnection(connectionString);
await connection.OpenAsync(ct);
await DoWorkAsync(connection, ct);

// ✓ using declaration (C# 8+)
await using var stream = new FileStream(path, FileMode.Open);
var content = await ReadAsync(stream, ct);

// ✓ Více zdrojů
await using var connection = await _dataSource.OpenConnectionAsync(ct);
await using var command = connection.CreateCommand();
command.CommandText = "SELECT ...";
await using var reader = await command.ExecuteReaderAsync(ct);
```

### HttpClient — správná správa životního cyklu

```csharp
// ✗ Nikdy nevytvářej HttpClient přímo v metodě — socket exhaustion
public async Task<string> FetchAsync(string url)
{
    using var client = new HttpClient();  // PROBLÉM — vyčerpání socketů
    return await client.GetStringAsync(url);
}

// ✓ IHttpClientFactory přes DI
public class OrderApiClient(IHttpClientFactory factory)
{
    public async Task<OrderDto?> GetOrderAsync(int id, CancellationToken ct)
    {
        using var client = factory.CreateClient("OrderApi");
        var response = await client.GetAsync($"/orders/{id}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderDto>(ct);
    }
}

// registrace v Program.cs
builder.Services.AddHttpClient("OrderApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

### Alokace v hot paths — preferuj struct a stackalloc

```csharp
// ✓ Span<T> a stackalloc pro krátkodobé buffery (vyhni se heap alokaci)
public static bool TryParseOrderCode(ReadOnlySpan<char> input, out int orderId)
{
    Span<char> buffer = stackalloc char[20];
    input.TrimStart('O').CopyTo(buffer);
    return int.TryParse(buffer[..input.Length - 1], out orderId);
}

// ✓ ArrayPool pro větší buffery
var pool = ArrayPool<byte>.Shared;
var buffer = pool.Rent(4096);
try
{
    var bytesRead = await stream.ReadAsync(buffer, ct);
    Process(buffer.AsSpan(0, bytesRead));
}
finally
{
    pool.Return(buffer);
}

// ✓ StringBuilder recyklace v DI (singleton)
// — nebo prostě nový StringBuilder per-request (GC to zvládne)
```

### Detekce memory leaků

```csharp
// ✓ Přidej do projektu pro detekci IDisposable problémů:
// <PackageReference Include="Microsoft.Extensions.ObjectPool" />

// ✓ V testech: ověřuj že objekty jsou skutečně uvolněny
[Fact]
public async Task Handler_DisposesResources_AfterExecution()
{
    var disposable = new TrackableDisposable();
    await handler.HandleAsync(new Request(disposable));
    disposable.IsDisposed.Should().BeTrue();
}
```
