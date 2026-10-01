# C# — Code Style

## Obecná pravidla

- Indentace: **4 mezery** (ne taby)
- Maximální délka řádku: **120 znaků**
- Encoding: UTF-8 bez BOM
- Nové řádky: LF (Unix) nebo CRLF konzistentně dle `.editorconfig`
- Závorky `{` vždy na novém řádku (Allman style) — viz výjimka níže

## var vs. explicitní typ

```csharp
// ✓ var je OK — typ je zřejmý z pravé strany
var orders = new List<Order>();
var service = new OrderService(repo, logger);
var result = await _repository.GetByIdAsync(id);

// ✓ var je OK — LINQ výrazy
var activeOrders = orders.Where(o => o.Status == OrderStatus.Confirmed).ToList();

// ✗ var není OK — typ není zřejmý
var data = GetData();          // co je data?
var x = Process(input);        // co vrací Process?

// ✓ Explicitní typ
Order? order = await _repository.GetByIdAsync(id);
IReadOnlyList<OrderDto> orders = await _service.GetAllAsync();
```

## Nullable reference types

Musí být povoleno v `.csproj`:
```xml
<Nullable>enable</Nullable>
```

```csharp
// ✓ Správně — explicitně označuj nullable
public record Order(int Id, string CustomerName, string? Notes);

public async Task<Order?> GetByIdAsync(int id)  // může vrátit null
{
    return await _context.Orders.FindAsync(id);
}

// ✓ Null-conditional a null-coalescing
var name = customer?.Name ?? "Neznámý";
var count = orders?.Count ?? 0;

// ✗ Vyhni se null-forgiving operátoru bez komentáře
var order = GetOrder()!;  // proč jsi si jistý, že to není null?
```

## Primary constructors (C# 12+)

```csharp
// ✓ Správně pro jednoduché třídy a services
public class OrderService(IOrderRepository repository, ILogger<OrderService> logger)
    : IOrderService
{
    public async Task<Order?> GetByIdAsync(int id, CancellationToken ct = default)
        => await repository.GetByIdAsync(id, ct);
}

// Pro složitější třídy s logikou v konstruktoru preferuj klasický styl
```

## Expression-bodied members

```csharp
// ✓ Vhodné pro jednoduché property a krátké metody
public string FullName => $"{FirstName} {LastName}";
public bool IsValid => Amount > 0 && !string.IsNullOrWhiteSpace(CustomerName);

public override string ToString() => $"Order #{Id} ({Status})";

// ✗ Nevhodné pro složitější logiku — sniž čitelnost
public async Task<Order> CreateAsync(CreateOrderRequest request) =>
    await _validator.ValidateAsync(request) is { IsValid: true }
        ? await _repository.CreateAsync(MapToOrder(request))
        : throw new ValidationException("...");  // toto je moc — použij klasický styl
```

## String interpolace a formatting

```csharp
// ✓ Interpolace pro jednoduché případy
var message = $"Objednávka #{order.Id} od {order.CustomerName}";

// ✓ StringBuilder pro opakované spojování (smyčky, 10+ operací)
var sb = new StringBuilder();
foreach (var item in items)
    sb.AppendLine($"- {item.Name}: {item.Price:C}");

// ✓ Raw string literals (C# 11+) pro víceřádkový text / JSON
var json = """
    {
        "id": 1,
        "name": "Test"
    }
    """;

// ✗ String concatenation v smyčce
var result = "";
foreach (var item in items)
    result += item.Name;  // O(n²) alokace
```

## Collection expressions (C# 12+)

```csharp
// ✓ Moderní syntaxe
int[] numbers = [1, 2, 3, 4, 5];
List<string> names = ["Alice", "Bob", "Charlie"];
IReadOnlyList<Order> empty = [];

// Spread operator
int[] combined = [..first, ..second];
```

## Pattern matching

```csharp
// ✓ Switch expression místo if-else řetězců
var discount = order.Status switch
{
    OrderStatus.Pending   => 0m,
    OrderStatus.Confirmed => 0.05m,
    OrderStatus.Shipped   => 0.10m,
    _                     => 0m
};

// ✓ Property pattern
if (order is { Status: OrderStatus.Confirmed, Total: > 1000 })
{
    ApplyLoyaltyDiscount(order);
}

// ✓ List patterns (C# 11+)
if (args is [var first, var second, ..])
{
    Process(first, second);
}
```

## LINQ

```csharp
// ✓ Method syntax preferována před query syntax
var result = orders
    .Where(o => o.Status == OrderStatus.Confirmed)
    .OrderByDescending(o => o.CreatedAt)
    .Select(o => new OrderDto(o.Id, o.CustomerName, o.Total, o.Status))
    .ToList();

// ✓ Materializuj IQueryable co nejpozději (lazy evaluation)
IQueryable<Order> query = _context.Orders.AsNoTracking();

if (filter.Status.HasValue)
    query = query.Where(o => o.Status == filter.Status.Value);

if (!string.IsNullOrWhiteSpace(filter.CustomerName))
    query = query.Where(o => o.CustomerName.Contains(filter.CustomerName));

var orders = await query
    .OrderByDescending(o => o.CreatedAt)
    .Take(filter.PageSize)
    .ToListAsync(ct);

// ✗ Neprovádět N+1 dotazy
foreach (var order in orders)
{
    var customer = await _context.Customers.FindAsync(order.CustomerId);  // N+1!
}

// ✓ Použij Include / projections
var orders = await _context.Orders
    .Include(o => o.Customer)
    .Select(o => new OrderDto(o.Id, o.Customer.Name, o.Total, o.Status))
    .ToListAsync(ct);
```

## Formátování (`.editorconfig`)

Přidej do rootu projektu:

```ini
root = true

[*.cs]
indent_style = space
indent_size = 4
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true
max_line_length = 120

# Naming rules (viz naming.md)
# Pořadí v souboru nerozhoduje — specifičtější pravidlo (víc modifikátorů) má přednost

# const a static readonly → PascalCase
dotnet_naming_rule.constants_should_be_pascal_case.symbols = constant_fields
dotnet_naming_rule.constants_should_be_pascal_case.style = pascal_case_style
dotnet_naming_rule.constants_should_be_pascal_case.severity = warning

dotnet_naming_symbols.constant_fields.applicable_kinds = field
dotnet_naming_symbols.constant_fields.applicable_accessibilities = *
dotnet_naming_symbols.constant_fields.required_modifiers = const

dotnet_naming_rule.static_readonly_should_be_pascal_case.symbols = static_readonly_fields
dotnet_naming_rule.static_readonly_should_be_pascal_case.style = pascal_case_style
dotnet_naming_rule.static_readonly_should_be_pascal_case.severity = warning

dotnet_naming_symbols.static_readonly_fields.applicable_kinds = field
dotnet_naming_symbols.static_readonly_fields.applicable_accessibilities = *
dotnet_naming_symbols.static_readonly_fields.required_modifiers = static, readonly

dotnet_naming_style.pascal_case_style.capitalization = pascal_case

# ostatní privátní fieldy (instanční i mutable static) → _camelCase
dotnet_naming_rule.private_fields_should_be_camel_case.symbols = private_fields
dotnet_naming_rule.private_fields_should_be_camel_case.style = camel_case_underscore_style
dotnet_naming_rule.private_fields_should_be_camel_case.severity = warning

dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private

dotnet_naming_style.camel_case_underscore_style.required_prefix = _
dotnet_naming_style.camel_case_underscore_style.capitalization = camel_case

# Prefer expression-bodied members
csharp_style_expression_bodied_properties = true:suggestion
csharp_style_expression_bodied_methods = when_on_single_line:suggestion

# Prefer primary constructors
csharp_style_prefer_primary_constructors = true:suggestion

# var preferences
csharp_style_var_for_built_in_types = false:warning
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = false:suggestion
```
