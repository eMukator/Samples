# CQRS — Command Query Responsibility Segregation

Oddělení zápisu (commands přes doménový model) od čtení (queries přímo do DB s projekcí). Navazuje na `ddd/`, `design-principles/`.

## Moduly

@cqrs-basics.md
@cqrs-pipeline.md
@cqrs-vertical-slices.md

## Rychlá kontrola

- [ ] Command mění stav a vrací nejvýše ID nebo `Result` — nikdy celý read model
- [ ] Query nemění stav, používá `AsNoTracking` + `Select` projekci do DTO
- [ ] Query nepoužívá repository ani doménové entity — čte rovnou z `DbContext` (nebo Dapper)
- [ ] Jeden handler = jeden use case; handler nevolá jiný handler
- [ ] Command handler: načti agregát → zavolej doménovou metodu → `SaveChangesAsync` (jednou)
- [ ] Validace vstupu v decoratoru / validatoru, business pravidla v doméně
- [ ] Názvy: `CreateOrderCommand` / `GetOrderDetailQuery` — rozkazovací způsob pro commandy, `Get…` pro queries
- [ ] Žádný MediatR / AutoMapper v nových projektech bez vědomého rozhodnutí o licenci
- [ ] Kód feature v jedné složce (vertical slice)

## Kdy CQRS NEpoužívat

- Čistý CRUD, kde čtení = zápis 1:1 → stačí service s metodami (`csharp/architecture.md`).
- **Oddělená read databáze** (jiný storage, projekce přes eventy) jen při prokázané potřebě výkonu — výchozí CQRS = **jedna DB, dva způsoby přístupu**.
