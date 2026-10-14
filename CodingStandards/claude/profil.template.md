# Profil <PROJEKT> pro agenty

<!-- Umístění: <projekt>/.claude/agentic/profil.md. Obecní agenti z ~/.claude/agents
     a příkazy /manager, /navrh ho čtou a jeho obsah má přednost před jejich obecnými postupy.
     Pravidla kódu patří do CLAUDE.md; sem jen postupy, checklist a poučení.
     Prázdnou sekci smaž. Komentáře po vyplnění smaž. -->

## Obecné

<!-- Čtou všichni agenti. -->
- Build: `<příkaz> > .scratch/<složka>/build.log 2>&1`, log čti nástrojem Read. <!-- předexistující varování, se kterými se nemá nic dělat -->
- Pracovní soubory: `.scratch/<slug>/` (sdílené v rámci zadání), `.scratch/<slug>/<úkol>/` (jen pro úkol). Do kořene `.scratch/` nic nezakládej a nic v něm nemaž.
- Mimo zájem agentů: <!-- migrace, generované soubory, složky mimo kompilaci; zápis blokuje hook (protectedPaths) -->
- Generované soubory, které mění každý build: <!-- např. app.css; kdy se commitují, kdy vracet -->

## Spuštění aplikace (tester, ui-ux)

Port **<port>**. Neměň ho. <!-- proč: uložená hesla, rezervace portů… -->

1. Build v popředí (viz Obecné).
2. Bash s parametrem `run_in_background: true`: `<příkaz spuštění>`. Nikdy `--no-build`.
3. Start ověř: `curl -s -o /dev/null -w "%{http_code}" http://localhost:<port>/` → `200`.
4. Po ověření ukonči (`TaskStop`; jinak `taskkill /PID` jen vlastního PID) a ověř `curl` → `000`.

### Účty

<!-- Oblast aplikace → testovací účet. Hesla sem nepiš; přihlášení autofillem prohlížeče nebo odkaz na soubor mimo repo. -->
| Oblast | Účet |
|---|---|

## Testy (vývojář, tester)

- `<příkaz testů> > .scratch/<složka>/test.log 2>&1`, jedna třída `<filtr>`. <!-- infrastruktura, kterou testy potřebují -->
- Úkol mění datový model a migrace ještě neexistuje → testy nespouštěj, napiš to do hlášení.
- Známé pády, které nejsou regresí: <!-- testy závislé na síti apod. -->

## Checklist reviewera

<!-- Jen pravidla, která jdou ověřit nad diffem. Každý bod s ID a odkazem na zdroj (CLAUDE.md, ADR, sada standardů). -->
- **Architektura:**
- **Data:**
- **UI a texty:**
- **Testy:** testy pokrývají akceptační kritéria, která jdou automatizovat.
- **Bezpečnost:** žádná tajemství v diffu.
- **Rozsah:** nic nad rámec úkolu, žádný spekulativní refaktoring ani nová abstrakce bez potřeby, žádný mrtvý kód.

## Manažer a commity

- Stav běhu: `.claude/runs/<slug>/stav.md` (patří do `.gitignore`).
- Zpráva commitu: <!-- jazyk, prefixy, povinné/zakázané řádky -->
- Větev: commit na aktuální větev, nikdy push.
- Hotová zadání: <!-- kam přesouvat, jen po souhlasu -->
- Po posledním úkolu: <!-- verze, changelog… -->

## Poučení

<!-- Zapisuje manažer na konci běhu, až po schválení uživatelem. Nejnovější nahoře.
     Formát: jedna věta „co příště udělat jinak“ + (slug zadání, měsíc).
     Poučení, které platí pro každý projekt, převeď do obecné definice agenta a odsud smaž. -->
