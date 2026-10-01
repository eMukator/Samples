# Domain-Driven Design

Platí pro projekty s **netriviální business logikou**. Navazuje na `design-principles/`, doplňuje `cqrs/` a `event-driven/`.

## Moduly

@ddd-tactical.md
@ddd-persistence.md
@ddd-strategic.md

## Rychlá kontrola

- [ ] Entity nemají public settery — stav se mění jen metodami s doménovým názvem (`order.Confirm()`, ne `order.Status = …`)
- [ ] Invarianty se kontrolují v konstruktoru / factory metodě / doménové metodě — nelze vytvořit nevalidní objekt
- [ ] Value objects jako `record` / `readonly record struct`, immutable, s validací
- [ ] Strongly-typed IDs (`OrderId`), ne holé `Guid` / `int`
- [ ] Kolekce vystavené jako `IReadOnlyCollection<T>`, změny jen přes aggregate root
- [ ] Mezi agregáty jen odkaz přes ID, nikdy navigační property na jiný agregát
- [ ] Jedna transakce = jeden agregát (ostatní přes domain events → eventual consistency)
- [ ] Repository jen pro aggregate roots, žádný generický `IRepository<T>`
- [ ] Domain projekt nemá reference na EF Core, ASP.NET ani jiné infrastrukturní balíčky
- [ ] Názvy tříd a metod odpovídají jazyku byznysu (ubiquitous language)

## Kdy DDD NEpoužívat

- CRUD formuláře nad tabulkami bez pravidel → stačí `csharp/architecture.md`.
- Reporting, exporty, read-only obrazovky → přímé dotazy/projekce (viz `cqrs/`).
- Kombinovat lze: bohatý model jen pro jádro domény (core domain), zbytek jednoduše.
