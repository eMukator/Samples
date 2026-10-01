# ADR — Architecture Decision Records

Krátké záznamy o **významných rozhodnutích** a jejich důvodech. Pro lidi i AI agenty: zabrání opakovanému otevírání vyřešených otázek a „opravám" záměrných kompromisů.

## Moduly

@adr-template.md

## Rychlá kontrola

- [ ] ADR jsou v repozitáři v `docs/adr/NNNN-nazev-v-kebab-case.md`, číslované vzestupně
- [ ] Jeden ADR = jedno rozhodnutí
- [ ] Obsahuje kontext, zvažované varianty, rozhodnutí a **důsledky** (i negativní)
- [ ] Přijatý ADR se **nemění** — změna rozhodnutí = nový ADR se stavem `Supersedes NNNN`, starý dostane `Superseded by NNNN`
- [ ] ADR vzniká v PR spolu se změnou, kterou zavádí
- [ ] Před architektonickou změnou si AI agent přečte `docs/adr/` a respektuje přijatá rozhodnutí; rozpor s ADR hlásí, neobchází

## Kdy ADR psát

- Volba architektonického stylu, frameworku, databáze, brokeru, knihovny se závazkem (licence, lock-in)
- Vědomá odchylka od sady pravidel v `c:\@DEV\_ai` (např. „Application smí referencovat EF Core")
- Rozhodnutí, které je drahé vrátit, nebo které se v týmu opakovaně diskutuje
- Bezpečnostní kompromis s akceptovaným rizikem

## Kdy NE

- Běžné implementační volby, které jdou snadno změnit (název proměnné, struktura jedné třídy)
- Věci, které už určuje sada pravidel bez odchylky
