# CQRS — Vertical Slice organizace

Kód organizuj **podle feature (use case)**, ne podle technické vrstvy. Změna jedné funkce = jedna složka.

## Struktura

```
src/MyApp/
  Domain/                          ← agregáty, value objects, domain events (sdílené napříč features)
    Orders/Order.cs
    Orders/OrderLine.cs
  Features/
    Orders/
      ConfirmOrder/
        ConfirmOrderCommand.cs     ← jeden typ = jeden soubor (csharp/naming.md)
        ConfirmOrderHandler.cs
        ConfirmOrderValidator.cs
        ConfirmOrderEndpoint.cs
      GetOrderDetail/
        GetOrderDetailQuery.cs
        GetOrderDetailHandler.cs
        GetOrderDetailEndpoint.cs
      OrdersEndpoints.cs           ← MapGroup("/api/orders") a volání Map* jednotlivých features
  Infrastructure/
    Persistence/AppDbContext.cs
    Persistence/Configurations/
```

- Jeden typ = jeden soubor (viz `csharp/naming.md`); feature drží pohromadě **složka**, ne soubor.
- Feature složky na sobě **nezávisí**. Sdílený kód patří do `Domain/` nebo do `Common/`.
- Duplicita mezi slices (podobné DTO) je **v pořádku** — slices se vyvíjí nezávisle (viz `design-principles/dp-pragmatism.md`).

---

## Endpoint per feature

```csharp
// ConfirmOrderEndpoint.cs
internal static class ConfirmOrderEndpoint
{
    public static RouteGroupBuilder MapConfirmOrder(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/confirm", HandleAsync)
            .RequireAuthorization("Orders.Write")
            .WithName("ConfirmOrder")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        ICommandHandler<ConfirmOrderCommand, Result<OrderId>> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new ConfirmOrderCommand(new OrderId(id)), ct);
        return result.IsSuccess ? Results.NoContent() : result.Error!.ToProblem();
    }
}

// OrdersEndpoints.cs
public static class OrdersEndpoints
{
    public static void MapOrders(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/orders").WithTags("Orders");
        group.MapConfirmOrder();
        group.MapGetOrderDetail();
    }
}
```

✗ Nezaváděj reflexní auto-registraci endpointů (`IEndpoint` + scan) — explicitní seznam je čitelnější a kompilátor ho hlídá.

---

## Vertical slices vs. vrstvy

| | Vrstvy (`csharp/architecture.md`) | Vertical slices |
|-|------|------|
| Vhodné pro | Malé CRUD aplikace, tým zvyklý na vrstvy | Rostoucí aplikace s mnoha use cases |
| Změna feature | Soubory v 4–5 projektech | Jedna složka |
| Riziko | Tlusté `*Service` třídy | Duplicita (vědomě akceptovaná) |

Lze kombinovat: projekty `Domain` / `Infrastructure` zůstávají, `Application` vrstva se organizuje do `Features/`.

---

## Architektonické testy

```csharp
[Fact]
public void Features_ShouldNotReferenceEachOther()
{
    var features = new[] { "MyApp.Features.Orders", "MyApp.Features.Customers", "MyApp.Features.Invoices" };
    foreach (var feature in features)
    {
        var others = features.Where(f => f != feature).ToArray();
        var result = Types.InAssembly(typeof(Program).Assembly)
            .That().ResideInNamespace(feature)
            .ShouldNot().HaveDependencyOnAny(others)
            .GetResult();
        Assert.True(result.IsSuccessful, $"{feature} závisí na jiné feature: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}

[Fact]
public void Domain_ShouldNotDependOn_Infrastructure()
{
    var result = Types.InAssembly(typeof(Order).Assembly)
        .ShouldNot().HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
        .GetResult();
    Assert.True(result.IsSuccessful);
}
```
