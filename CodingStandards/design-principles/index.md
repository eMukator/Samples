# Návrhové principy — SOLID, coupling, pragmatismus

Platí pro veškerý C# kód. Principy jsou nástroj, ne cíl — každá abstrakce musí platit nájem.

## Moduly

@dp-solid.md
@dp-coupling-cohesion.md
@dp-pragmatism.md

## Rychlá kontrola

- [ ] Třída má jeden důvod ke změně (SRP) — název bez „And", „Manager", „Helper", „Utils"
- [ ] Žádný `switch` / `if` řetězec nad typem nebo enumem opakovaný na více místech (OCP → polymorfismus)
- [ ] Odvozená třída nevyhazuje `NotSupportedException` pro zděděnou metodu (LSP)
- [ ] Interface je úzký a definovaný z pohledu konzumenta (ISP)
- [ ] Závislosti přes konstruktor, žádné `new` na službách, žádný service locator (DIP)
- [ ] Třídy `sealed` jako výchozí, dědičnost jen záměrně
- [ ] Interface s jedinou implementací jen tam, kde je to hranice vrstvy nebo test seam
- [ ] Žádný kód „pro budoucnost" (YAGNI) — žádné nepoužité parametry, konfigurace, generické vrstvy

## Kdy pravidla nepoužívat dogmaticky

- Skripty, prototypy, jednorázové nástroje — přímočarý kód je správně.
- Jednoduchý CRUD bez business logiky — vrstvy a interfaces navíc jsou čistá režie.
- DRY nad nesouvisejícími věcmi — dva podobné kusy kódu, které se mění z různých důvodů, **nejsou** duplicita.
