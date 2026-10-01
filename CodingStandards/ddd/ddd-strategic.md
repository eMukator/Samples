# DDD — Strategický návrh

## Ubiquitous Language

Kód používá **stejná slova jako byznys**. Ne technické překlady.

```csharp
// ✗ Technický jazyk
order.SetStatus(3);
customerService.UpdateFlag(customerId, true);

// ✓ Jazyk domény
order.Ship(trackingNumber);
customer.Blacklist(reason);
```

- Slovník pojmů veď v `docs/glossary.md` projektu (CZ termín → název v kódu → definice).
- Stejné slovo může v různých kontextech znamenat různé věci — to je signál hranice bounded contextu.

---

## Bounded Context

Hranice, uvnitř které má model jednoznačný význam.

```
„Produkt" v kontextu:
  Katalog   → název, popis, fotky, kategorie
  Sklad     → SKU, množství, umístění
  Fakturace → cena, DPH sazba
```

✓ Tři různé třídy `Product` ve třech kontextech — **ne** jedna obří sdílená entita.

### Mapování na .NET

| Velikost systému | Bounded context = |
|------------------|-------------------|
| Malý | Složka / namespace v jednom projektu |
| Střední | Modul modulárního monolitu (vlastní projekty, vlastní `DbContext` a DB schéma) |
| Velký | Samostatná služba |

```
src/
  Modules/
    Catalog/
      Catalog.Domain/
      Catalog.Application/
      Catalog.Infrastructure/     ← CatalogDbContext, schema "catalog"
      Catalog.Contracts/          ← jediné, co smí ostatní moduly referencovat
    Ordering/
      ...
```

Pravidla modulárního monolitu:
- Modul **nesahá do tabulek** jiného modulu ani na jeho `DbContext`.
- Komunikace jen přes `*.Contracts` (synchronní dotaz přes interface, nebo integration event).
- Dodržování hlídej architektonickými testy (NetArchTest / ArchUnitNET) v CI.

```csharp
[Fact]
public void Ordering_ShouldNotDependOn_CatalogInternals()
{
    var result = Types.InAssembly(typeof(Order).Assembly)
        .ShouldNot()
        .HaveDependencyOnAny("Catalog.Domain", "Catalog.Application", "Catalog.Infrastructure")
        .GetResult();

    Assert.True(result.IsSuccessful);
}
```

---

## Context Mapping — vztahy mezi kontexty

| Vzor | Kdy |
|------|-----|
| **Shared Kernel** | Malý sdílený kód (např. `Money`) — vlastní ho oba týmy, mění se jen dohodou. Minimalizuj. |
| **Customer–Supplier** | Downstream kontext ovlivňuje priority upstream API. |
| **Conformist** | Downstream přebírá model upstreamu bez úprav (typicky externí SaaS, na který nemáš vliv). |
| **Anti-Corruption Layer** | Downstream překládá cizí model na vlastní — chrání doménu před cizími pojmy. |
| **Published Language** | Dobře zdokumentovaný sdílený formát (integration events, OpenAPI). |

---

## Anti-Corruption Layer

Každá integrace s externím / legacy systémem jde přes ACL. Cizí DTO **nikdy** neproniknou do domény.

```csharp
// Application — kontrakt v jazyce naší domény
public interface IShippingGateway
{
    Task<TrackingNumber> CreateShipmentAsync(Order order, Address destination, CancellationToken ct);
}

// Infrastructure — ACL překládá z/do modelu dopravce
public sealed class PplShippingGateway(PplApiClient client) : IShippingGateway
{
    public async Task<TrackingNumber> CreateShipmentAsync(Order order, Address destination, CancellationToken ct)
    {
        var request = new PplShipmentRequest   // cizí model
        {
            RecipientZip = destination.PostalCode.Value,
            ParcelCount = 1,
            ReferenceId = order.Id.ToString()
        };
        var response = await client.CreateAsync(request, ct);

        return response.Status == "OK"
            ? new TrackingNumber(response.ShipmentNumber)
            : throw new ShippingFailedException(response.ErrorDescription);
    }
}
```

---

## Core / Supporting / Generic subdomény

| Typ | Příklad | Investice |
|-----|---------|-----------|
| **Core** | Cenotvorba, plánování — konkurenční výhoda | Bohatý DDD model, nejlepší lidé, testy |
| **Supporting** | Správa skladových lokací | Jednoduchý model, CRUD je OK |
| **Generic** | Autentizace, e-maily, platby | Kup / použij hotové (Identity, SaaS) |

✗ Plné DDD na generické subdoméně je plýtvání.
