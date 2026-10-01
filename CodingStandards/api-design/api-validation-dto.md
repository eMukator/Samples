# API — Validace & Request/Response Vzory

## Validace vstupů — FluentValidation

```csharp
// NuGet: FluentValidation.AspNetCore

// CreateOrderRequestValidator.cs
public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("Jméno zákazníka je povinné.")
            .MaximumLength(100).WithMessage("Jméno nesmí být delší než 100 znaků.")
            .Matches(@"^[\p{L}\s\-'.]+$").WithMessage("Jméno obsahuje nepovolené znaky.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress().WithMessage("E-mail není platný.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Objednávka musí obsahovat alespoň jednu položku.")
            .Must(items => items.Count <= 50).WithMessage("Maximum je 50 položek.");

        RuleForEach(x => x.Items).SetValidator(new OrderItemValidator());

        RuleFor(x => x.DeliveryDate)
            .GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow))
            .When(x => x.DeliveryDate.HasValue)
            .WithMessage("Datum doručení musí být v budoucnosti.");
    }
}

public class OrderItemValidator : AbstractValidator<CreateOrderItemRequest>
{
    public OrderItemValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).InclusiveBetween(1, 999);
    }
}
```

```csharp
// Program.cs — registrace
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderRequestValidator>();
```

Kde validaci spouštět — **jen na jednom místě**:

| Projekt | Spuštění validátoru |
|---------|---------------------|
| CQRS (handlery) | `ValidationDecorator` kolem command handlerů (viz `cqrs/cqrs-pipeline.md`) |
| Minimal API bez CQRS | Endpoint filter volající `IValidator<T>` |
| MVC controllery | `AddFluentValidationAutoValidation()` — balíček `FluentValidation.AspNetCore` je autory označen jako zastaralý a funguje jen pro MVC; pro nový kód preferuj variantu výše |

✗ Auto-validace v MVC **a zároveň** validační decorator — dvojí validace, dva formáty chyb.

### Manuální validace ve service (pro business rules)

```csharp
public class OrderService(IValidator<CreateOrderRequest> validator, ...)
{
    public async Task<Result<OrderDto>> CreateAsync(
        CreateOrderRequest request, CancellationToken ct)
    {
        // Fluent validace
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<OrderDto>.Failure(validation.Errors
                .Select(e => e.ErrorMessage).ToList());

        // Business rules validace
        var customer = await _customerRepo.GetByEmailAsync(request.Email, ct);
        if (customer is null)
            return Result<OrderDto>.Failure("Zákazník nebyl nalezen.");

        if (customer.IsBlocked)
            return Result<OrderDto>.Failure("Zákazník má zablokovaný účet.");

        // ...
    }
}
```

---

## Request/Response DTOs — konvence

```csharp
// ✓ Pojmenování: {Akce}{Entita}{Request/Response}
public record CreateOrderRequest(
    string CustomerName,
    string Email,
    IReadOnlyList<CreateOrderItemRequest> Items,
    DateOnly? DeliveryDate = null,
    string? Note = null);

public record CreateOrderItemRequest(
    int ProductId,
    int Quantity);

public record OrderDto(
    int          Id,
    string       CustomerName,
    string       Email,
    OrderStatus  Status,
    decimal      Total,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OrderItemDto> Items);

public record OrderItemDto(
    int     Id,
    string  ProductName,
    int     Quantity,
    decimal UnitPrice,
    decimal LineTotal);

// ✓ Partial update — nullable pole
public record UpdateOrderRequest(
    string?     CustomerName = null,
    string?     Note         = null,
    DateOnly?   DeliveryDate = null);
```

### Datum a čas v API

```csharp
// ✓ Vždy ISO 8601 UTC v JSON
// "createdAt": "2025-01-15T14:30:00Z"

// Program.cs — nastavení JSON serializace
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy    = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition  = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

// ✓ DateTimeOffset místo DateTime — zachovává timezone info
public record OrderDto(
    int            Id,
    DateTimeOffset CreatedAt,   // "2025-01-15T14:30:00+01:00" nebo "2025-01-15T13:30:00Z"
    DateOnly?      DeliveryDate // "2025-02-01" — jen datum bez času
);
```

---

## Idempotency — bezpečné opakování požadavků

POST operace s dopadem (platby, objednávky) přijímají hlavičku `Idempotency-Key`. Implementace viz `resilience/res-idempotency.md`.

✗ Prostá cache „klíč → odpověď" kontrolovaná před zpracováním — dva souběžné požadavky se stejným klíčem projdou oba, klíč není vázaný na uživatele a nekontroluje se shoda těla požadavku.

---

## Zdravé vzory pro controller

```csharp
// ✓ Slim controller — žádná business logika
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<OrderDto>>> GetAll(
        [FromQuery] CursorPageRequest request, CancellationToken ct)
        => Ok(await orderService.GetPagedAsync(request, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetById(int id, CancellationToken ct)
    {
        var order = await orderService.GetByIdAsync(id, ct);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(
        CreateOrderRequest request, CancellationToken ct)
    {
        var result = await orderService.CreateAsync(request, ct);
        if (!result.IsSuccess) return BadRequest(result.Error);

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id, UpdateOrderStatusRequest request, CancellationToken ct)
    {
        var result = await orderService.UpdateStatusAsync(id, request.Status, ct);
        return result.IsSuccess ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await orderService.DeleteAsync(id, ct);
        return NoContent();
    }
}
```
