# Pravidlo 6 — Kontroluj všechny návratové hodnoty

> *"Každá funkce která může selhat musí mít zkontrolovanou návratovou hodnotu. Pokud výsledek záměrně ignoruješ, explicitně to okomentuj."*

## C# adaptace

### Tasks a async — nikdy nezapomenut await

```csharp
// ✗ Fire-and-forget bez ošetření chyb — výjimka je spolknuta tiše
_emailService.SendConfirmationAsync(order);  // CHYBA — Task je ignorován

// ✗ Uložení do proměnné bez awaitu také nestačí
var task = _emailService.SendConfirmationAsync(order);  // stále ignorováno

// ✓ Await vždy
await _emailService.SendConfirmationAsync(order, ct);

// ✓ Pokud práce nemá blokovat request: nespouštěj Task bez await,
//   ale zařaď ji do Outboxu (nesmí se ztratit) nebo Channel<T> (smí se ztratit)
//   — viz csharp/async.md, event-driven/ed-outbox-inbox.md
```

### Result pattern — explicitní ošetření chyb

```csharp
// ✓ Pokud metoda vrací Result, musíš zkontrolovat IsSuccess
var result = await _orderService.CreateAsync(request, ct);

// ✗ Použití hodnoty bez kontroly
var dto = result.Value!;  // může být null když IsSuccess == false

// ✓ Vždy kontroluj před použitím
if (!result.IsSuccess)
{
    _logger.LogWarning("Order creation failed: {Error}", result.Error);
    return BadRequest(result.Error);
}

var dto = result.Value!;  // zde je Value garantovaně non-null
return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
```

### TryParse a TryGet vzory

```csharp
// ✗ Parse bez kontroly
var id = int.Parse(input);  // výjimka při neplatném vstupu

// ✓ TryParse — výsledek musí být zkontrolován
if (!int.TryParse(input, out var id))
{
    return BadRequest($"Neplatné ID: '{input}'");
}

// ✗ Dictionary přístup bez kontroly
var value = _dict[key];  // KeyNotFoundException

// ✓ TryGetValue
if (!_dict.TryGetValue(key, out var value))
{
    return null;  // nebo výchozí hodnota
}
```

### EF Core — SaveChanges

```csharp
// ✗ SaveChanges bez kontroly počtu ovlivněných řádků
await _context.SaveChangesAsync(ct);  // OK pokud ti nezáleží na výsledku...

// ✓ Kdy záleží na výsledku — zkontroluj
var affected = await _context.SaveChangesAsync(ct);

if (affected == 0)
{
    _logger.LogWarning("No rows affected when updating order {OrderId}", order.Id);
    throw new OrderUpdateException(order.Id, "Nebyla aktualizována žádná data");
}

// ✓ Optimistická konkurence — zachyť DbUpdateConcurrencyException
try
{
    await _context.SaveChangesAsync(ct);
}
catch (DbUpdateConcurrencyException ex)
{
    _logger.LogWarning(ex, "Concurrency conflict for order {OrderId}", order.Id);
    throw new OrderConflictException(order.Id);
}
```

### HttpClient responses

```csharp
// ✗ Ignorování HTTP status kódu
var response = await _httpClient.GetAsync(url, ct);
var data = await response.Content.ReadFromJsonAsync<OrderDto>(ct);  // null pokud 404

// ✓ Vždy zkontroluj status
var response = await _httpClient.GetAsync(url, ct);

if (response.StatusCode == HttpStatusCode.NotFound)
    return null;

if (!response.IsSuccessStatusCode)
{
    var body = await response.Content.ReadAsStringAsync(ct);
    throw new ExternalApiException(url, (int)response.StatusCode, body);
}

return await response.Content.ReadFromJsonAsync<OrderDto>(ct);
```

### Záměrné ignorování — musí být dokumentováno

```csharp
// ✓ Výsledek ignorován vědomě — použij discard s komentářem
_ = cache.TryAdd(key, value);  // Nevadí pokud key existuje — závodní podmínka je OK

// ✓ Nebo okomentuj inline
var removed = _dict.Remove(key);
// removed = false je OK — idempotentní operace, klíč mohl být odstraněn dříve
```
