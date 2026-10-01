# Coupling & Cohesion

## Composition over inheritance

```csharp
// ✗ Špatně — hluboká hierarchie kvůli sdílení kódu
public class BaseService { protected void Log(...) { } protected void Validate(...) { } }
public class OrderService : BaseService { }
public class PremiumOrderService : OrderService { }   // 3 úrovně, křehké

// ✓ Správně — skládání přes DI
public sealed class OrderService(ILogger<OrderService> logger, IValidator<CreateOrder> validator) { }
```

- Třídy `sealed` jako výchozí (i drobný výkonový přínos — devirtualizace).
- Dědičnost jen pro skutečný vztah „je" a pro framework (`ControllerBase`, `ComponentBase`, `DbContext`).
- Max. 2 úrovně vlastní hierarchie.

---

## Law of Demeter — „mluv jen se sousedy"

```csharp
// ✗ Špatně — volající zná vnitřní strukturu tří objektů
var city = order.Customer.Address.City;
if (order.Customer.Account.Balance >= order.Total) { ... }

// ✓ Správně — zeptej se objektu, nesahej do něj („Tell, don't ask")
if (order.Customer.CanAfford(order.Total)) { ... }
```

Výjimka: DTO a read modely — tam jsou řetězené navigace v pořádku (jsou to data, ne chování).

---

## Vysoká koheze

Co se mění spolu, patří k sobě.

```
// ✗ Organizace podle technického typu — změna jedné funkce = 5 složek
Controllers/OrdersController.cs
Services/OrderService.cs
Validators/CreateOrderValidator.cs
Dtos/CreateOrderRequest.cs
Mappers/OrderMapper.cs

// ✓ Organizace podle feature (viz cqrs/cqrs-vertical-slices.md)
Features/Orders/CreateOrder/
    CreateOrderCommand.cs
    CreateOrderHandler.cs
    CreateOrderValidator.cs
    CreateOrderEndpoint.cs
```

---

## Nízký coupling — signály problému

| Signál | Náprava |
|--------|---------|
| Konstruktor s > 5 závislostmi | Rozděl třídu (SRP) |
| Změna v A vynutí změnu v B, C, D | Chybí abstrakce nebo je špatná hranice |
| Cyklická závislost projektů / namespaců | Vytáhni sdílený kontrakt, nebo slouč |
| Statické třídy se stavem | Nahraď službou v DI |
| `internal` typy viditelné přes `InternalsVisibleTo` všude | Špatné hranice assembly |

---

## Primitive obsession

```csharp
// ✗ Špatně — snadno prohodíš parametry, validace roztroušená
public Task TransferAsync(Guid fromId, Guid toId, decimal amount, string currency);

// ✓ Správně — typy nesou význam i validaci (viz ddd/ddd-tactical.md)
public Task TransferAsync(AccountId from, AccountId to, Money amount);
```
