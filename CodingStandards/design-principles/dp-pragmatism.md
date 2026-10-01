# Pragmatismus — YAGNI, KISS, DRY správně

## YAGNI — You Aren't Gonna Need It

Implementuj **aktuální požadavek**, ne odhadovanou budoucnost.

```csharp
// ✗ Špatně — spekulativní obecnost
public interface IRepository<T, TKey> where T : IEntity<TKey> { /* 15 metod */ }
public interface INotificationChannelFactory { INotificationChannel Create(ChannelType type); }
// ... a existuje jen EmailChannel

// ✓ Správně — přímé řešení, abstrakce až s druhou implementací
public sealed class EmailNotifier(IEmailService email) { ... }
```

Pravidlo tří: abstrakci zaváděj až u **třetího** výskytu (u druhého maximálně poznamenej).

---

## Kdy interface s jednou implementací ANO

| Situace | Interface? |
|---------|-----------|
| Hranice vrstvy (Application ↔ Infrastructure) | ✓ ano — DIP |
| Externí systém (e-mail, platby, HTTP API, čas) | ✓ ano — test seam / fake |
| Doménová služba bez I/O | ✗ ne — testuj přímo |
| Handler, validator, mapper | ✗ ne |
| „Pro případ, že by…" | ✗ ne |

---

## KISS — nejjednodušší řešení, které funguje

Pořadí, ve kterém hledej řešení:

1. Je to vůbec potřeba?
2. Existuje to už v projektu?
3. Řeší to BCL (.NET knihovna)? — `System.Text.Json`, `TimeProvider`, `Channel<T>`, `FrozenDictionary`, `HybridCache`, `IHttpClientFactory`, `Microsoft.Extensions.Resilience`
4. Řeší to již nainstalovaný balíček?
5. Teprve pak vlastní kód / nový NuGet.

```csharp
// ✗ Špatně — vlastní mapper framework pro 3 DTO
// ✓ Správně — ruční mapování, kompilátor hlídá chybějící property
public static OrderDto ToDto(this Order o) => new(o.Id.Value, o.CustomerName, o.Total.Amount);
```

> AutoMapper a MediatR jsou od 2025 pod komerční licencí. Pro nové projekty preferuj ruční mapování (nebo Mapperly — source generator, MIT) a viz `cqrs/` pro dispatcher.

---

## DRY — duplicita znalosti, ne textu

```csharp
// Toto NENÍ porušení DRY — dvě validace vypadají stejně, ale patří různým pravidlům
public sealed class CreateCustomerValidator { RuleFor(x => x.Name).MaximumLength(100); }
public sealed class CreateSupplierValidator  { RuleFor(x => x.Name).MaximumLength(100); }
// Když dodavatel dostane limit 200, změní se jen jeden.

// Toto JE porušení DRY — stejné business pravidlo na dvou místech
if (order.Total > 10_000) requireApproval = true;   // v OrderHandler
if (order.Total > 10_000) badge = "Ke schválení";   // v Razor komponentě
// ✓ Pravidlo patří doméně: order.RequiresApproval
```

---

## Checklist před zavedením abstrakce

- [ ] Mám **dnes** alespoň dvě implementace nebo test, který ji potřebuje?
- [ ] Zjednoduší to kód volajícího, nebo jen přidá skok navíc?
- [ ] Pochopí to kolega ve 3 ráno při incidentu?
- [ ] Neřeší to už framework?
