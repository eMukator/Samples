# CQRS — Pipeline (cross-cutting concerns)

Průřezové věci (validace, logování, metriky) **neopakuj v každém handleru** — obal handler decoratorem.

## Registrace (Scrutor)

```csharp
// Program.cs / AddApplication()
services.Scan(scan => scan
    .FromAssemblyOf<ConfirmOrderHandler>()
    .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
        .AsImplementedInterfaces().WithScopedLifetime()
    .AddClasses(c => c.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
        .AsImplementedInterfaces().WithScopedLifetime());

// Pořadí: poslední registrovaný decorator je vnější
services.Decorate(typeof(ICommandHandler<,>), typeof(ValidationDecorator<,>));
services.Decorate(typeof(ICommandHandler<,>), typeof(LoggingDecorator<,>));

services.AddValidatorsFromAssemblyOf<ConfirmOrderHandler>(includeInternalTypes: true);   // FluentValidation
```

Výsledný řetězec: `Logging → Validation → Handler`.

---

## Validační decorator

```csharp
internal sealed class ValidationDecorator<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    IEnumerable<IValidator<TCommand>> validators)
    : ICommandHandler<TCommand, TResult>
{
    public async Task<TResult> HandleAsync(TCommand command, CancellationToken ct)
    {
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(command, ct);
            if (!result.IsValid)
                throw new ValidationException(result.Errors);   // middleware → 400 ValidationProblemDetails
        }
        return await inner.HandleAsync(command, ct);
    }
}
```

| Kde validovat | Co |
|---------------|-----|
| Validator (decorator) | Tvar vstupu: povinná pole, délky, formáty, rozsahy |
| Doména | Business invarianty: „prázdnou objednávku nelze potvrdit" |
| Handler | Existence záznamů (`NotFound`), oprávnění k objektu |

---

## Logovací decorator

```csharp
internal sealed class LoggingDecorator<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    ILogger<LoggingDecorator<TCommand, TResult>> logger)
    : ICommandHandler<TCommand, TResult>
{
    public async Task<TResult> HandleAsync(TCommand command, CancellationToken ct)
    {
        var name = typeof(TCommand).Name;
        var started = Stopwatch.GetTimestamp();
        try
        {
            var result = await inner.HandleAsync(command, ct);
            logger.LogInformation("Command {Command} finished in {ElapsedMs} ms",
                name, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Command {Command} failed", name);
            throw;
        }
    }
}
```

- ✗ Nelogujte celý command — může obsahovat PII (viz `observability/`).
- Pro metriky a tracing přidej `ActivitySource` span se jménem commandu.

---

## Transakce

Výchozí: **handler volá `uow.SaveChangesAsync(ct)` explicitně jednou** — EF `SaveChanges` je sám transakční, domain events běží uvnitř (viz `ddd/ddd-persistence.md`).

Explicitní transakci (`BeginTransactionAsync`) použij jen pokud:
- handler musí volat `SaveChanges` vícekrát (např. kvůli generovaným ID u legacy schématu), nebo
- kombinuješ EF s raw SQL / Dapperem ve stejném zápisu.

✗ Nezaváděj „TransactionDecorator" kolem všech commandů preventivně.

---

## Autorizace

```csharp
// ✓ Endpoint — role / policy
group.MapPost("/{id:guid}/confirm", ...).RequireAuthorization("Orders.Write");

// ✓ Handler — oprávnění k KONKRÉTNÍMU objektu (resource-based)
if (order.CustomerId != currentUser.CustomerId) return Error.NotFound("Objednávka");   // neprozrazuj existenci
```
