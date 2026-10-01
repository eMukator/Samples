# Výkon — paměť a CPU

Platí pro **hot paths** (kód volaný v každém requestu / ve smyčkách nad velkými daty). Jinde má přednost čitelnost.

## Async — blokování thread poolu

```csharp
// ✗ Sync-over-async — thread pool starvation pod zátěží, deadlocky
var order = repository.GetAsync(id, ct).Result;
service.SaveAsync(order).Wait();

// ✗ Task.Run v ASP.NET Core — jen přesouvá práci na jiné vlákno téhož poolu
return await Task.Run(() => Calculate(order));

// ✓ Async až dolů
var order = await repository.GetAsync(id, ct);
```

- `ValueTask<T>` jen u metod, které **většinou končí synchronně** (cache hit) a jsou velmi často volané. Jinak `Task<T>`.
- ✗ `ValueTask` neawaituj dvakrát ani neukládej.

---

## Alokace

```csharp
// ✓ Kapacita kolekce, když znáš velikost
var result = new List<OrderDto>(orders.Count);
var map = new Dictionary<int, Order>(orders.Count);

// ✓ StringBuilder pro skládání ve smyčce
var sb = new StringBuilder(capacity: 256);
foreach (var line in lines) sb.Append(line.Name).Append(';');

// ✗ Konkatenace ve smyčce — O(n²) alokací
var s = "";
foreach (var line in lines) s += line.Name + ";";
```

### Span<T> a stackalloc

```csharp
// ✓ Parsování bez alokace substringů
ReadOnlySpan<char> input = "2026-10-01;123;CZK";
var firstSep = input.IndexOf(';');
var date = DateOnly.Parse(input[..firstSep]);
var rest = input[(firstSep + 1)..];

// ✓ Malý dočasný buffer na zásobníku (do ~1 KB)
Span<byte> hash = stackalloc byte[32];
SHA256.HashData(data, hash);
var hex = Convert.ToHexString(hash);
```

### ArrayPool

```csharp
// ✓ Velké dočasné buffery — půjčit a vrátit
var buffer = ArrayPool<byte>.Shared.Rent(81920);
try
{
    int read;
    while ((read = await source.ReadAsync(buffer, ct)) > 0)
        await target.WriteAsync(buffer.AsMemory(0, read), ct);
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}
```

- ✗ Pole > 85 KB alokované opakovaně → Large Object Heap, drahé Gen 2 GC.
- Pronajaté pole může být **větší** než požadované — vždy pracuj s délkou, kterou jsi zapsal.

---

## LINQ v hot paths

```csharp
// ✗ Ve smyčce volané milionkrát — alokace enumerátorů a delegátů, opakovaný průchod
var hasActive = items.Where(i => i.IsActive).Count() > 0;
var first = items.Where(i => i.Price > 100).FirstOrDefault();

// ✓
var hasActive = items.Any(i => i.IsActive);
var first = items.FirstOrDefault(i => i.Price > 100);
```

- ✗ `.Count()` na `IEnumerable` vícekrát / opakovaná enumerace → materializuj jednou (`ToList()`) nebo použij `.Count` property.
- V běžném business kódu je LINQ v pořádku — tohle platí pro hot paths.

---

## Neměnné lookupy

```csharp
// ✓ .NET 8+ — sestavení dražší, čtení rychlejší; pro data, která se po startu nemění
private static readonly FrozenDictionary<string, VatRate> VatRates = new Dictionary<string, VatRate>
{
    ["CZ"] = new(21m), ["SK"] = new(23m), ["DE"] = new(19m)
}.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

// ✓ .NET 8+ — rychlé hledání sady znaků / řetězců
private static readonly SearchValues<char> Separators = SearchValues.Create(";,|\t");
var idx = input.AsSpan().IndexOfAny(Separators);
```

---

## Source generatory

```csharp
// ✓ Regex — kompilace při buildu, žádná reflexe za běhu
[GeneratedRegex(@"^\d{3}\s?\d{2}$", RegexOptions.CultureInvariant)]
private static partial Regex PostalCodeRegex();

// ✓ Logování bez boxingu a parsování šablony při každém volání
[LoggerMessage(Level = LogLevel.Information, Message = "Order {OrderId} confirmed, total {Total}")]
private static partial void LogOrderConfirmed(ILogger logger, Guid orderId, decimal total);

// ✓ System.Text.Json source gen — rychlejší (de)serializace, nutné pro Native AOT
[JsonSerializable(typeof(OrderDto))]
[JsonSerializable(typeof(List<OrderDto>))]
internal partial class AppJsonContext : JsonSerializerContext;

builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default));
```

---

## Výjimky

```csharp
// ✗ Výjimky pro řízení toku v hot path — drahé (stack trace)
try { var id = int.Parse(input); } catch (FormatException) { return null; }

// ✓
return int.TryParse(input, out var id) ? id : null;
```

Viz též `csharp/error-handling.md` a Result pattern v `cqrs/cqrs-basics.md`.

---

## Typy

- `sealed` třídy — JIT může devirtualizovat volání (viz `design-principles/`).
- `readonly record struct` pro malé (≤ ~16 B), neměnné hodnoty často vytvářené ve velkém (strongly-typed ID).
- ✗ Velké struktury předávané hodnotou — kopírování. Velký struct → `in` parametr nebo class.
- ✗ Boxing: struct přes `object` / neparametrický interface v hot path.
