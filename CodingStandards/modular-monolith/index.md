# Modulární monolit

Jedna nasaditelná aplikace rozdělená na **autonomní moduly** podle bounded contextů (viz `ddd/ddd-strategic.md`). Mezikrok mezi monolitem a mikroslužbami.

## Moduly

@mm-structure.md
@mm-communication.md

## Rychlá kontrola

- [ ] Modul = bounded context, ne technická vrstva („Ordering", ne „Repositories")
- [ ] Každý modul má vlastní `DbContext`, vlastní DB schéma a vlastní migrace
- [ ] Žádné cross-schema joiny ani cizí klíče mezi moduly
- [ ] Ostatní moduly smí referencovat **jen** `*.Contracts` projekt modulu
- [ ] Vše kromě Contracts a registrační metody je `internal`
- [ ] Synchronní volání přes rozhraní v Contracts, asynchronní přes integration events (Outbox)
- [ ] Žádná transakce přes více modulů — eventual consistency
- [ ] Hranice hlídají architektonické testy v CI
- [ ] Modul se registruje jednou metodou (`AddOrderingModule`, `MapOrderingEndpoints`)

## Kdy NEpoužívat

- Malá aplikace s jednou oblastí domény → Clean architecture v jednom řezu.
- Moduly bez jasných hranic (vše souvisí se vším) → nejdřív ujasni doménu (event storming), pak dělit.
