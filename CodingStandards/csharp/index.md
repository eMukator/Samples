# C# Coding Standards — ASP.NET Core

Platí pro všechny C# soubory v projektu. Při každé změně kódu dodržuj VŠECHNA pravidla níže.

## Moduly

@naming.md
@code-style.md
@architecture.md
@async.md
@error-handling.md
@testing.md

## Rychlá kontrola

- [ ] Naming odpovídá konvencím (PascalCase třídy, camelCase lokální proměnné)
- [ ] Žádné `var` kde typ není zřejmý z pravé strany
- [ ] Async metody mají suffix `Async`
- [ ] Žádné `async void` mimo event handlery
- [ ] Výjimky jsou specifické, nikdy `catch (Exception e) {}`  bez důvodu
- [ ] Nullable reference types jsou zapnuty (`<Nullable>enable</Nullable>`)
- [ ] Žádné magic strings/numbers — použij konstanty nebo enums
- [ ] Unit testy pro každou novou business logiku — fake implementace místo mocků (viz `testing.md`)
- [ ] Privátní fieldy `_camelCase`, `const` a `static readonly` PascalCase
