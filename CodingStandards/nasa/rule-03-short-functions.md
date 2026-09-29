# Pravidlo 3 — Funkce musí být krátké

> *"Žádná funkce nesmí být delší než se vejde na jeden list A4 — přibližně 60 řádků."*

## C# adaptace

**Limit: 60 řádků na metodu.** Komentáře a prázdné řádky se počítají.

Pokud metodu nelze přečíst celou bez scrollování, je příliš dlouhá. Dlouhé metody skrývají:
- příliš mnoho zodpovědností (Single Responsibility porušeno)
- příležitosti pro znovupoužití
- složité podmínkové větve, které patří do samostatných metod

### Signály že metoda je příliš dlouhá

```csharp
// ✗ God method — dělá příliš mnoho
public async Task<IActionResult> ProcessOrder(CreateOrderRequest request)
{
    // validace (10 řádků)
    if (string.IsNullOrWhiteSpace(request.CustomerName)) ...
    if (request.Items == null || !request.Items.Any()) ...
    foreach (var item in request.Items) { if (item.Quantity <= 0) ... }

    // výpočet ceny (15 řádků)
    var subtotal = request.Items.Sum(i => i.Price * i.Quantity);
    var tax = subtotal * 0.21m;
    var discount = ...;
    var total = subtotal + tax - discount;

    // uložení (10 řádků)
    var order = new Order { ... };
    _context.Orders.Add(order);
    await _context.SaveChangesAsync();

    // notifikace (15 řádků)
    var email = new EmailMessage { ... };
    await _emailService.SendAsync(email);

    // logging (5 řádků)
    _logger.LogInformation(...);

    return Ok(order);
    // = 55+ řádků, 4 zodpovědnosti
}
```

### Správná dekompozice

```csharp
// ✓ Orchestrující metoda — jen koordinuje, nepřesahuje 20 řádků
public async Task<Result<OrderDto>> ProcessOrderAsync(
    CreateOrderRequest request,
    CancellationToken ct)
{
    var validationResult = _validator.Validate(request);
    if (!validationResult.IsValid)
        return Result<OrderDto>.Failure(validationResult.ErrorMessage);

    var pricing = await _pricingService.CalculateAsync(request.Items, ct);
    var order = await _orderRepository.CreateAsync(
        MapToOrder(request, pricing), ct);

    await _notificationService.SendOrderConfirmationAsync(order, ct);

    _logger.LogInformation("Order {OrderId} created for {Customer}",
        order.Id, order.CustomerName);

    return Result<OrderDto>.Success(MapToDto(order));
}

// ✓ Každá zodpovědnost je vlastní metoda / service
private Order MapToOrder(CreateOrderRequest request, PricingResult pricing)
    => new()
    {
        CustomerName = request.CustomerName,
        Items = request.Items.Select(MapToOrderItem).ToList(),
        Subtotal = pricing.Subtotal,
        Tax = pricing.Tax,
        Discount = pricing.Discount,
        Total = pricing.Total,
        CreatedAt = _clock.UtcNow
    };
```

### Metrika složitosti — Cyclomatic Complexity

Každá metoda by měla mít Cyclomatic Complexity ≤ 10.
Každý `if`, `else`, `case`, `&&`, `||`, `?`, `catch`, `for`, `while`, `foreach` přidává 1.

```csharp
// CC = 7 — hraniční, zvažuj refactoring
public decimal CalculateDiscount(Order order, Customer customer)
{
    if (customer.IsVip)                          // +1
    {
        if (order.Total > 10_000)                // +1
            return order.Total * 0.20m;
        else if (order.Total > 5_000)            // +1
            return order.Total * 0.15m;
        else
            return order.Total * 0.10m;
    }
    else if (customer.OrderCount > 10)           // +1
    {
        return order.Total > 2_000               // +1
            ? order.Total * 0.05m
            : 0m;
    }
    return 0m;
}

// ✓ Refaktorováno — switch expression, CC = 3
public decimal CalculateDiscount(Order order, Customer customer)
    => (customer.IsVip, order.Total, customer.OrderCount) switch
    {
        (true, > 10_000, _) => order.Total * 0.20m,
        (true, > 5_000, _)  => order.Total * 0.15m,
        (true, _, _)        => order.Total * 0.10m,
        (false, > 2_000, > 10) => order.Total * 0.05m,
        _                   => 0m
    };
```

### Nastavení analyzátoru pro délku metod

```xml
<!-- .editorconfig -->
dotnet_diagnostic.MA0051.severity = warning   # Meziantis.Analyzers: method too long
```

Nebo přidej do projektu:
```xml
<PackageReference Include="Meziantis.Analyzers" Version="*" PrivateAssets="all" />
```
