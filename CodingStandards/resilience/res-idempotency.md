# Resilience — idempotence

Klient při timeoutu neví, jestli se operace provedla. Opakuje. Server musí opakování ustát **bez dvojího efektu** (dvě objednávky, dvě platby).

## HTTP metody

| Metoda | Idempotentní? | Retry bezpečný? |
|--------|---------------|-----------------|
| GET, HEAD, OPTIONS | ✓ | ✓ |
| PUT (celý zdroj), DELETE | ✓ (z definice) | ✓, pokud je implementace skutečně idempotentní |
| POST | ✗ | Jen s `Idempotency-Key` |
| PATCH | ✗ obecně | Jen s klíčem nebo podmíněně (`If-Match` + ETag) |

```csharp
// ✗ DELETE, který vrací 404 při opakování, je OK; DELETE, který při opakování smaže „další" záznam, ne
// ✗ PUT, který inkrementuje čítač, není idempotentní
```

---

## Idempotency-Key pro POST

Klient pošle unikátní klíč (UUID) v hlavičce. Server si uloží výsledek a při opakování ho vrátí.

```http
POST /api/v1/payments
Idempotency-Key: 0192f4c1-7a3e-7b8a-9c1d-2e3f4a5b6c7d
Content-Type: application/json
```

```csharp
public sealed class IdempotencyRecord
{
    public required string Key { get; init; }            // PK spolu s UserId
    public required string UserId { get; init; }
    public required string RequestHash { get; init; }    // SHA-256 těla — detekce zneužití klíče
    public int? StatusCode { get; set; }                  // null = zpracovává se
    public string? ResponseBody { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}

internal sealed class IdempotencyFilter(AppDbContext db, TimeProvider clock) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var http = ctx.HttpContext;
        if (!http.Request.Headers.TryGetValue("Idempotency-Key", out var key) || !Guid.TryParse(key, out _))
            return Results.Problem("Chybí nebo je neplatná hlavička Idempotency-Key.", statusCode: 400);

        var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var hash = await ComputeBodyHashAsync(http.Request);

        var existing = await db.IdempotencyRecords.FindAsync([key.ToString(), userId], http.RequestAborted);
        if (existing is not null)
        {
            if (existing.RequestHash != hash)
                return Results.Problem("Klíč byl použit s jiným požadavkem.", statusCode: 422);
            if (existing.StatusCode is null)
                return Results.Problem("Požadavek se stále zpracovává.", statusCode: 409);
            return Results.Text(existing.ResponseBody ?? "", "application/json", statusCode: existing.StatusCode);
        }

        db.IdempotencyRecords.Add(new() { Key = key!, UserId = userId, RequestHash = hash, CreatedAt = clock.GetUtcNow() });
        await db.SaveChangesAsync(http.RequestAborted);   // souběžný duplikát → PK violation → 409

        var result = await next(ctx);
        // … ulož StatusCode + ResponseBody výsledku (serializace IResult dle projektu)
        return result;
    }
}
```

- Klíč je vázaný na **uživatele** — cizí klíč nesmí vrátit cizí odpověď.
- Záznamy s `StatusCode = null` starší než N minut (pád uprostřed) uvolni pro nový pokus.
- Retence 24 h – 7 dní; úklid úlohou.
- Ukládej jen úspěšné a business-chybové odpovědi (2xx, 4xx). Při 5xx záznam smaž — klient může zkusit znovu.
- Hotová implementace: knihovny existují, ale filtr výše na pár endpointů stačí (platby, objednávky).

---

## Přirozená idempotence — preferuj

Lepší než ukládat odpovědi je navrhnout operaci tak, aby opakování nic nezměnilo:

```csharp
// ✓ Klient generuje ID — opakovaný POST se stejným ID nevytvoří duplikát (unique constraint)
POST /api/v1/orders   { "orderId": "0192f4c1-…", "lines": [...] }

// ✓ Nastavení stavu místo relativní změny
order.MarkAsPaid(paymentId);       // opakování = no-op
// ✗ account.Balance += amount;

// ✓ Upsert s podmínkou verze
UPDATE … SET Status = @new, Version = Version + 1 WHERE Id = @id AND Version = @expected
```

---

## Volání cizích API

- Pokud cizí API podporuje idempotency klíč (Stripe, GoPay, …), **vždy ho posílej** — odvoď ho deterministicky z naší entity (např. `payment-{PaymentId}`), ne náhodně per pokus.
- Při nejistém výsledku (timeout) se před opakováním zeptej na stav (`GET /payments?reference=…`).
- Konzumenti zpráv: viz Inbox v `event-driven/ed-outbox-inbox.md`.
