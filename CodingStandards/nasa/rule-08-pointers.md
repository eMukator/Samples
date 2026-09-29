# Pravidlo 8 — Omez hloubku zanoření a řetězení

> *"Nepoužívej ukazatele přes více než jednu úroveň dereference. Víc úrovní je nečitelné a neauditovatelné."*

## C# adaptace

V C# neexistují raw pointery (běžně), ale stejný problém se projevuje jako:
- hluboké řetězení objektů: `order.Customer.Address.City.Name`
- hluboce zanořené `if` bloky (arrow anti-pattern)
- více než 3 úrovně zanoření kódu obecně

### Pravidlo hloubky přístupu k objektům

```csharp
// ✗ Demeter's Law porušení — "train wreck"
var city = order.Customer.Address.City.Name;
var discount = order.Customer.LoyaltyProgram.CurrentTier.DiscountRate;

// Problémy:
// 1. NullReferenceException na kterémkoli místě řetězce
// 2. Úzká vazba na interní strukturu Customer, Address, City
// 3. Nelze mockovat v testech

// ✓ Maximálně 2 úrovně přístupu
var customer = order.Customer;
var city = customer.Address.City;  // 2 úrovně — OK

// ✓ Nebo encapsuluj do metody / property na správné třídě
public string GetDeliveryCity() => Customer.Address.City;  // v Order třídě
var city = order.GetDeliveryCity();  // 1 úroveň z pohledu volajícího
```

### Hloubka zanoření kódu — max 3 úrovně

```csharp
// ✗ Arrow anti-pattern — příliš hluboké zanoření
public async Task ProcessOrdersAsync(IEnumerable<Order> orders, CancellationToken ct)
{
    foreach (var order in orders)                           // úroveň 1
    {
        if (order.Status == OrderStatus.Pending)            // úroveň 2
        {
            foreach (var item in order.Items)               // úroveň 3
            {
                if (item.Stock > 0)                         // úroveň 4 ✗
                {
                    if (item.Price > 0)                     // úroveň 5 ✗✗
                    {
                        await ProcessItemAsync(item, ct);
                    }
                }
            }
        }
    }
}

// ✓ Guard clauses a early return snižují zanoření
public async Task ProcessOrdersAsync(IEnumerable<Order> orders, CancellationToken ct)
{
    foreach (var order in orders)
        await ProcessSingleOrderAsync(order, ct);
}

private async Task ProcessSingleOrderAsync(Order order, CancellationToken ct)
{
    if (order.Status != OrderStatus.Pending) return;  // early return

    var processableItems = order.Items
        .Where(item => item.Stock > 0 && item.Price > 0);

    foreach (var item in processableItems)
        await ProcessItemAsync(item, ct);
}
```

### LINQ řetězení — čitelné dělení

```csharp
// ✗ Jedno dlouhé řetězení přes více řádků bez struktury
var result = orders.Where(o => o.Status == OrderStatus.Confirmed && o.CreatedAt > cutoff).OrderByDescending(o => o.Total).GroupBy(o => o.Customer.Region).Select(g => new RegionSummary(g.Key, g.Sum(o => o.Total), g.Count())).Where(s => s.Total > 10_000).ToList();

// ✓ Pojmenované kroky nebo rozdělení do proměnných
var recentConfirmed = orders
    .Where(o => o.Status == OrderStatus.Confirmed)
    .Where(o => o.CreatedAt > cutoff);

var byRegion = recentConfirmed
    .GroupBy(o => o.Customer.Region)
    .Select(g => new RegionSummary(g.Key, g.Sum(o => o.Total), g.Count()));

var significantRegions = byRegion
    .Where(s => s.Total > 10_000)
    .OrderByDescending(s => s.Total)
    .ToList();
```

### Nullable chains — optional chaining s limitem

```csharp
// ✓ Optional chaining je OK pro max 3 úrovně
var city = order?.Customer?.Address?.City;

// ✗ Hlubší řetězení je signál špatného designu
var zip = order?.Customer?.Address?.City?.PostalCode?.Code;

// ✓ Refaktoruj — přesuň zodpovědnost na správné místo
// V Customer třídě:
public string? GetDeliveryPostalCode() => Address?.City?.PostalCode?.Code;

var zip = order.Customer?.GetDeliveryPostalCode();
```

### Vnořené generiky — max 2 úrovně

```csharp
// ✗ Nečitelné generické typy
Dictionary<string, List<Dictionary<int, OrderStatus>>> status;

// ✓ Named type aliases nebo vlastní třídy
using OrderStatusByRegion = Dictionary<string, List<(int OrderId, OrderStatus Status)>>;

// nebo lépe — vlastní typ
public record RegionOrderStatus(string Region, IReadOnlyList<OrderStatusEntry> Entries);
public record OrderStatusEntry(int OrderId, OrderStatus Status);
```
