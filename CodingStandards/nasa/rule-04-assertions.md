# Pravidlo 4 — Aserce a guard clauses

> *"Minimálně 2 aserce na každých 10 řádků kódu. Aserce nesmí mít side effects. Při selhání se systém zastaví nebo bezpečně zotaví."*

## C# adaptace

V C# máme několik úrovní "asercí" podle závažnosti:

| Mechanismus | Kdy použít |
|-------------|-----------|
| `Guard clauses` (throw) | Validace veřejného vstupu — vždy |
| `Debug.Assert` | Interní invarianty — pouze v Debug buildu |
| `ArgumentException` family | Neplatné argumenty od volajícího |
| `InvalidOperationException` | Neplatný stav objektu |
| Unit test assertions | Ověření chování v testech |

### Guard clauses — vstupní validace

```csharp
// ✓ Každá veřejná metoda validuje vstupy na začátku
public async Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken ct)
{
    ArgumentNullException.ThrowIfNull(request);
    ArgumentException.ThrowIfNullOrWhiteSpace(request.CustomerName);
    ArgumentOutOfRangeException.ThrowIfZero(request.Items.Count);

    // ... zbytek metody — od tohoto bodu jsou vstupy důvěryhodné
}

// ✓ .NET 8+ — ArgumentException helpers
ArgumentNullException.ThrowIfNull(value);                    // null check
ArgumentException.ThrowIfNullOrWhiteSpace(str);              // prázdný string
ArgumentOutOfRangeException.ThrowIfNegative(count);          // záporné číslo
ArgumentOutOfRangeException.ThrowIfZero(count);              // nula
ArgumentOutOfRangeException.ThrowIfGreaterThan(count, max);  // překročení maxima
```

### Guard helper pro doménové podmínky

```csharp
// ✓ Vlastní Guard třída pro business invarianty
public static class Guard
{
    public static void Against<TException>(bool condition, string message)
        where TException : Exception
    {
        if (condition)
        {
            var ex = (TException)Activator.CreateInstance(typeof(TException), message)!;
            throw ex;
        }
    }

    public static T NotNull<T>(T? value, string paramName) where T : class
        => value ?? throw new ArgumentNullException(paramName);

    public static void ValidId(int id, string paramName = "id")
        => ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id, paramName);

    public static void ValidEnum<T>(T value, string paramName) where T : struct, Enum
    {
        if (!Enum.IsDefined(value))
            throw new ArgumentOutOfRangeException(paramName, $"Neplatná hodnota enumu: {value}");
    }
}

// Použití
public async Task<Order> UpdateStatusAsync(int orderId, OrderStatus newStatus, CancellationToken ct)
{
    Guard.ValidId(orderId);
    Guard.ValidEnum(newStatus, nameof(newStatus));

    var order = await _repository.GetByIdAsync(orderId, ct)
        ?? throw new OrderNotFoundException(orderId);

    Guard.Against<InvalidOperationException>(
        order.Status == OrderStatus.Cancelled,
        $"Nelze změnit stav zrušené objednávky #{orderId}");

    // ...
}
```

### Debug.Assert — interní invarianty

```csharp
// ✓ Pro podmínky které NIKDY nesmí nastat v správném kódu
public decimal ApplyDiscount(decimal price, decimal discountRate)
{
    Debug.Assert(price >= 0, $"Cena nesmí být záporná: {price}");
    Debug.Assert(discountRate is >= 0 and <= 1,
        $"Sleva musí být 0–1: {discountRate}");

    var result = price * (1 - discountRate);

    Debug.Assert(result <= price, "Cena po slevě nesmí být vyšší než původní");

    return result;
}
```

### Aserce jsou bez side effects

```csharp
// ✗ Aserce se side effectem — stav se mění při assertu
Debug.Assert(_repository.Save(order).Id > 0);  // CHYBA — save se volá jen v Debug

// ✓ Aserce pouze kontroluje, nikdy nemění stav
var saved = await _repository.SaveAsync(order, ct);
Debug.Assert(saved.Id > 0, $"Save musí vrátit platné ID");
```

### Fail-fast princip

```csharp
// ✓ Při zjištění nekonzistentního stavu selhej okamžitě, hlasitě
public void ProcessPayment(Payment payment)
{
    if (payment.Amount <= 0)
        throw new InvalidOperationException(
            $"Neplatná platba: amount={payment.Amount}. Toto by nemělo nastat.");

    if (payment.Currency is not ("CZK" or "EUR" or "USD"))
        throw new InvalidOperationException(
            $"Neznámá měna: {payment.Currency}");

    // zpracuj platbu
}
```
