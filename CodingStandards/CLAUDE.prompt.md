Vytvoř CLAUDE.md pro tento repozitář. Postupuj takto:

1. Prozkoumej repo: package.json / Makefile / pyproject a podobné soubory, CI konfiguraci (.github/workflows apod.), README, docs/, konfigurace linteru a testů a posledních ~30 commitů v git logu.
2. Zjisti PŘESNÉ příkazy pro instalaci, build, spuštění, testy (včetně spuštění jednoho testu), lint a typecheck. Ověř je v CI configu.
3. Hledej věci, které nejsou na první pohled zřejmé: neobvyklé konvence, workaroundy, komentáře typu TODO/HACK/NOTE, speciální skripty, nestandardní postupy.
4. NEŽ začneš psát, polož mi 5–8 cílených otázek na věci, které z kódu nezjistíš: pasti, na co se nesahá, workflow pravidla a opakované chyby, které bych nechtěl, abys dělal.

Pravidla pro výsledný soubor:
- Maximálně ~60 řádků. Každý řádek musí projít testem: „Udělal bys bez něj chybu?" Pokud ne, vynech ho.
- NEPIŠ strom adresářů, výčet technologií zjistitelný z konfigurací, obecné rady o kvalitě kódu ani nic, co najdeš v kódu za pár vteřin.
- Piš stručné imperativní body, ne prózu.
- Dlouhé specifické informace nedávej do souboru. Místo nich napiš odkaz na dokument, který se má číst jen v relevantní situaci.
- Sekce: Příkazy, Pravidla, Pasti, případně Kontext. Prázdnou sekci vynech.

Na konci mi zvlášť vypiš věci, které jsi zvažoval, ale vynechal, abych mohl případně něco vrátit.

