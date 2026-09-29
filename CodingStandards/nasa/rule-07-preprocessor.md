# Pravidlo 7 — Omez metaprogramování a dynamický kód

> *"Nepoužívej preprocessor makra s kódem. Pouze konstanty a podmíněnou kompilaci."*

## C# adaptace

V C# neexistuje preprocessor jako v C, ale ekvivalentní rizika jsou:
- nekontrolovaná reflexe (`Type.GetMethod`, `Activator.CreateInstance`)
- dynamické generování kódu (`Expression.Compile`, `Reflection.Emit`)
- `dynamic` typ
- source generators bez porozumění výstupu

### dynamic — zakázán bez explicitního zdůvodnění

```csharp
// ✗ dynamic — ztráta typové bezpečnosti, pomalé, chyby až za běhu
dynamic order = GetOrder();
var total = order.Total;  // spadne pokud Total neexistuje — zjistíš až za běhu

// ✓ Správně typovaný kód
Order order = await GetOrderAsync(id, ct);
var total = order.Total;

// ✓ Pokud pracuješ s neznámou strukturou (JSON, konfigurace) — použij specifické typy
var config = _configuration.GetSection("Orders").Get<OrdersConfig>()
    ?? throw new InvalidOperationException("Chybí konfigurace Orders");
```

### Reflexe — jen kde není alternativa

```csharp
// ✗ Reflexe pro běžné operace — je pomalá a netypsafe
var method = typeof(OrderService).GetMethod("CreateAsync");
var result = method!.Invoke(service, new object[] { request, ct });

// ✓ Přímé volání
var result = await _orderService.CreateAsync(request, ct);

// ✓ Pokud reflexe je nezbytná (generické mapování, frameworková infrastruktura):
// — použij ji pouze v inicializační fázi, výsledky cachuj
private static readonly Dictionary<Type, PropertyInfo[]> _propertyCache = new();

private static PropertyInfo[] GetProperties(Type type)
    => _propertyCache.GetOrAdd(type, t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));
```

### Podmíněná kompilace — pouze pro prostředí

```csharp
// ✓ Podmíněná kompilace pouze pro debug/release přepínání
#if DEBUG
    _logger.LogDebug("Order details: {@Order}", order);
#endif

// ✗ Podmíněná kompilace pro business logiku
#if PREMIUM
    ApplyPremiumDiscount(order);  // CHYBA — business podmínky patří do kódu, ne kompilace
#endif

// ✓ Business podmínky řeš kódem, ne kompilací
if (_featureFlags.IsPremiumDiscountEnabled)
    ApplyPremiumDiscount(order);
```

### Source generators — ano, ale vědomě

```csharp
// ✓ Source generators jsou OK — generují kód v době kompilace, výstup je viditelný
// Příklady: System.Text.Json, Mapster, Dapper.AOT, Refit

[JsonSerializable(typeof(OrderDto))]
internal partial class OrderJsonContext : JsonSerializerContext { }

// ✓ Vždy zkontroluj generovaný kód (Analyzers/obj složka)
// — porozuměj co se generuje, nejde o "magie"
```

### AutoMapper — s rozmyslem

```csharp
// AutoMapper je komfort, ale skrývá mapovací logiku. Pravidla:
// ✓ Používej pouze pro jednoduché 1:1 mapování
// ✓ Komplexní mapování piš explicitně jako private metody
// ✓ Vždy přidej AssertConfigurationIsValid() do testů

// V testech:
[Fact]
public void AutoMapper_ConfigurationIsValid()
{
    var mapper = new MapperConfiguration(cfg => cfg.AddProfile<OrderMappingProfile>());
    mapper.AssertConfigurationIsValid();  // odhalí chybějící mapování
}
```
