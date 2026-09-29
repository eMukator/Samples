# JavaScript / TypeScript Coding Standards

Platí pro veškerý JS/TS kód — vanilla JS v ASP.NET Core projektech, Node skripty i bundlované moduly.

## Moduly

@js-style.md
@js-typescript.md
@js-async.md
@js-patterns.md

## Rychlá kontrola

- [ ] TypeScript zapnut kde je to možné (`strict: true`)
- [ ] Žádné `var` — pouze `const` nebo `let`
- [ ] `const` jako výchozí, `let` pouze pokud se hodnota mění
- [ ] Žádné `any` bez komentáře s vysvětlením
- [ ] Async funkce mají ošetřen error (try/catch nebo `.catch()`)
- [ ] Žádné `console.log` commitnuté do produkce
- [ ] Importy seřazeny: external → internal → relative
