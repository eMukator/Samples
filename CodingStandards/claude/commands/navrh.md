---
description: Dva nezávislé návrhy řešení (architekt A ∥ B) nad zadáním a jejich srovnání podle hodnoticí matice. Variantu vybírá uživatel; rozhodnutí se zapíše do zadání.
argument-hint: <cesta k zadání>
---

Zadání je v souboru `$1`. Slug = název souboru bez přípony.
Návrhy patří do `.claude/runs/<slug>/navrh-A.md` a `navrh-B.md`.

1. V **jedné zprávě** zavolej souběžně dvakrát subagenta `architekt`:
   - varianta A: zadání, cesta `.claude/runs/<slug>/navrh-A.md`;
   - varianta B: totéž s cestou `navrh-B.md` a větou „Zvaž i jiný přístup
     než ten nejpřímočařejší, pokud dává smysl.“
   Žádnému nezmiňuj obsah druhého návrhu.
2. Po dokončení obou si návrhy přečti a ukaž uživateli srovnání:

   | Kritérium (váha) | A | B |
   |---|---|---|
   | Konzistence architektury (50 %) | fakta | fakta |
   | Výkon (25 %) | | |
   | Rychlost vývoje (25 %) | | |
   | Migrace / dotčené soubory / rizika | | |

   Body nepřiděluj a variantu nevybírej – hodnotí uživatel. Upozorni na
   místa, kde se návrhy zásadně liší nebo kde oba přehlížejí totéž.
3. Po rozhodnutí uživatele zapracuj zvolené řešení do zadání jako aktuální
   stav (řešení + rozpad na úkoly s AC). Zamítnuté varianty, „mimo rozsah“
   seznamy ani historii rozhodování do zadání nepiš – svádí vykonavatele
   k driftu; zůstávají v `.claude/runs/<slug>/`. Hranici rozsahu zapiš
   jednou větou („mění se jen X“). Pak nabídni spuštění `/manager $1`.
