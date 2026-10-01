# Clean Architecture — styly a jejich vynucení

Volba architektonického stylu a **automatická kontrola** jeho pravidel v CI. Rozšiřuje `csharp/architecture.md`, navazuje na `ddd/`, `cqrs/`, `modular-monolith/`.

## Moduly

@ca-styles.md
@ca-enforcement.md

## Rychlá kontrola

- [ ] Styl architektury zvolen vědomě a zapsán v ADR (viz `adr/`)
- [ ] Závislosti míří dovnitř: Infrastructure/Web → Application → Domain
- [ ] Domain nereferencuje EF Core, ASP.NET Core, žádný infrastrukturní NuGet
- [ ] Application definuje rozhraní (porty), Infrastructure je implementuje (adaptéry)
- [ ] Směr závislostí hlídají architektonické testy v CI, ne jen dokumentace
- [ ] Typy jsou `internal` jako výchozí; `public` jen to, co tvoří API projektu
- [ ] Composition root (registrace DI) jen ve Web/Host projektu
- [ ] Počet projektů odpovídá velikosti systému — žádných 8 projektů pro CRUD

## Kdy NEzavádět Clean Architecture

- Malá aplikace / prototyp / interní nástroj → jeden projekt se složkami.
- Čistý CRUD bez business pravidel → vrstvy přidávají jen mapovací režii.
