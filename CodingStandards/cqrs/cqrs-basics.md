# CQRS — Základy

## Kontrakty handlerů

```csharp
public interface ICommandHandler<in TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct);
}

public interface IQueryHandler<in TQuery, TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct);
}
```

Interfaces existují kvůli decoratorům (validace, logování — viz `cqrs-pipeline.md`). Endpoint injektuje handler **přímo** — žádný mediator není potřeba.

---

## Výběr dispatcheru

| Varianta | Licence | Kdy |
|----------|---------|-----|
| **Přímá injekce handleru + Scrutor decoratory** | MIT | ✓ Výchozí volba. Nejjednodušší, explicitní, žádná magie. |
| `Mediator` (martinothamar, source generator) | MIT | Chceš `ISender.Send()` styl a pipeline behaviors. |
| Wolverine | MIT | Zároveň potřebuješ messaging, outbox, sagy (viz `event-driven/`). |
| MediatR 13+ | Komerční (zdarma do limitu obratu) | Existující projekty; nové jen s vědomým rozhodnutím. |

---

## Result

```csharp
public sealed record Error(string Code, string Message)
{
    public static Error NotFound(string what) => new("not_found", $"{what} nebyl nalezen.");
    public static Error Conflict(string message) => new("conflict", message);
    public static Error Validation(string message) => new("validation", message);
    public static Error Forbidden() => new("forbidden", "K této operaci nemáte oprávnění.");
}

public readonly record struct Result<T>
{
    public T? Value { get; }
    public Error? Error { get; }
    public bool IsSuccess => Error is null;

    private Result(T? value, Error? error) => (Value, Error) = (value, error);

    public static Result<T> Success(T value) => new(value, null);
    public static Result<T> Failure(Error error) => new(default, error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}

// Mapování na HTTP — jedno místo pro celé API (viz api-design/)
public static class ResultExtensions
{
    public static IResult ToProblem(this Error error) => error.Code switch
    {
        "not_found"  => Results.Problem(error.Message, statusCode: 404),
        "conflict"   => Results.Problem(error.Message, statusCode: 409),
        "validation" => Results.Problem(error.Message, statusCode: 400),
        "forbidden"  => Results.Problem(error.Message, statusCode: 403),
        _            => Results.Problem(error.Message, statusCode: 422)
    };
}
```

> Nahrazuje `Result<T>` se `string Error` z `csharp/architecture.md` — typovaný kód chyby umožňuje správný HTTP status.

---

## Command

```csharp
public sealed record ConfirmOrderCommand(OrderId OrderId);

public sealed class ConfirmOrderHandler(IOrderRepository orders, IUnitOfWork uow)
    : ICommandHandler<ConfirmOrderCommand, Result<OrderId>>
{
    public async Task<Result<OrderId>> HandleAsync(ConfirmOrderCommand command, CancellationToken ct)
    {
        var order = await orders.GetAsync(command.OrderId, ct);
        if (order is null) return Error.NotFound("Objednávka");

        order.Confirm();                    // business pravidla v doméně
        await uow.SaveChangesAsync(ct);     // jednou, na konci; spustí i domain events
        return order.Id;
    }
}
```

```csharp
// ✗ Špatně — logika v handleru místo v doméně (anemický model)
if (order.Status != OrderStatus.Draft) return Error.Conflict("...");
if (order.Lines.Count == 0) return Error.Validation("...");
order.Status = OrderStatus.Confirmed;

// ✗ Špatně — command vrací read model
public Task<OrderDetailDto> HandleAsync(ConfirmOrderCommand command, ...)
// Klient si po úspěchu zavolá query, nebo endpoint vrátí 201 + Location.

// ✗ Špatně — handler volá jiný handler
await _createInvoiceHandler.HandleAsync(new CreateInvoiceCommand(...), ct);
// ✓ Order raise OrderConfirmed → handler eventu vytvoří fakturu (viz event-driven/)
```

---

## Query

```csharp
public sealed record GetOrderDetailQuery(OrderId OrderId);

public sealed record OrderDetailDto(Guid Id, string CustomerName, string Status, decimal Total, IReadOnlyList<OrderLineDto> Lines);
public sealed record OrderLineDto(string ProductName, int Quantity, decimal UnitPrice);

public sealed class GetOrderDetailHandler(AppDbContext db)
    : IQueryHandler<GetOrderDetailQuery, OrderDetailDto?>
{
    public Task<OrderDetailDto?> HandleAsync(GetOrderDetailQuery query, CancellationToken ct)
        => db.Orders
            .AsNoTracking()
            .Where(o => o.Id == query.OrderId)
            .Select(o => new OrderDetailDto(
                o.Id.Value,
                db.Customers.Where(c => c.Id == o.CustomerId).Select(c => c.Name).First(),
                o.Status.ToString(),
                o.Lines.Sum(l => l.UnitPrice.Amount * l.Quantity),
                o.Lines.Select(l => new OrderLineDto(
                    db.Products.Where(p => p.Id == l.ProductId).Select(p => p.Name).First(),
                    l.Quantity,
                    l.UnitPrice.Amount)).ToList()))
            .FirstOrDefaultAsync(ct);
}
```

- Query **smí** číst napříč agregáty (join) — konzistenční hranice platí jen pro zápis.
- Pro složité reporty je v pořádku Dapper / raw SQL / databázový view — query side nemusí být EF.
- Seznamy vždy stránkované (viz `api-design/` — cursor pagination).
- Pozor na překlad do SQL: `.ToString()` na enumu s `HasConversion<string>()` se přeloží; vlastní metody ne — ověř v logu SQL.

---

## Endpoint (Minimal API)

```csharp
group.MapPost("/{id:guid}/confirm", async (
        Guid id,
        ICommandHandler<ConfirmOrderCommand, Result<OrderId>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new ConfirmOrderCommand(new OrderId(id)), ct);
        return result.IsSuccess ? Results.NoContent() : result.Error!.ToProblem();
    });

group.MapGet("/{id:guid}", async (
        Guid id,
        IQueryHandler<GetOrderDetailQuery, OrderDetailDto?> handler,
        CancellationToken ct) =>
        await handler.HandleAsync(new GetOrderDetailQuery(new OrderId(id)), ct) is { } dto
            ? Results.Ok(dto)
            : Results.NotFound());
```

Endpoint jen překládá HTTP ↔ command/query. Žádná logika.
