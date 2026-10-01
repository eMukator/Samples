# ADR — šablona a příklady

## Šablona

Ulož jako `docs/adr/NNNN-nazev.md`:

```markdown
# NNNN. Název rozhodnutí v rozkazovacím tvaru

- **Stav:** Navrženo | Přijato | Zamítnuto | Zastaralé | Nahrazeno [NNNN](NNNN-nazev.md)
- **Datum:** YYYY-MM-DD
- **Rozhodli:** jména / role

## Kontext

Jaký problém řešíme, jaká omezení platí (technická, business, tým, termín, rozpočet).
Fakta, ne obhajoba řešení.

## Zvažované varianty

1. **Varianta A** — stručně; + výhody, − nevýhody
2. **Varianta B** — …
3. **Nic nedělat** — vždy zvaž

## Rozhodnutí

Zvolili jsme **variantu X**, protože …

## Důsledky

- ✓ Pozitivní
- ✗ Negativní / přijaté riziko
- Co se musí udělat (migrace, úprava pravidel, školení)
- Kdy rozhodnutí přehodnotit (konkrétní spouštěč: „při > 3 instancích", „pokud licence změní podmínky")
```

Rozsah: typicky **jedna obrazovka**. Delší podklady (benchmarky, analýzy) odkaž, nevkládej.

---

## Index

`docs/adr/README.md`:

```markdown
# Architecture Decision Records

| # | Rozhodnutí | Stav |
|---|------------|------|
| [0001](0001-zaznamenavat-rozhodnuti.md) | Zaznamenávat architektonická rozhodnutí | Přijato |
| [0002](0002-modularni-monolit.md) | Modulární monolit místo mikroslužeb | Přijato |
| [0003](0003-bez-mediatr.md) | Handlery bez MediatR | Přijato |
```

---

## Příklad

```markdown
# 0003. Volat handlery přímo bez MediatR

- **Stav:** Přijato
- **Datum:** 2026-10-01
- **Rozhodli:** vývojový tým

## Kontext

Používáme CQRS (sada `cqrs/`). MediatR od verze 13 vyžaduje komerční licenci nad limit obratu.
Potřebujeme průřezovou validaci a logování. Tým má 3 vývojáře, aplikace ~60 use cases.

## Zvažované varianty

1. **MediatR 13** — + známé API, pipeline behaviors; − licence, runtime reflexe
2. **Mediator (source generator, MIT)** — + rychlý, kompatibilní styl; − další závislost, menší komunita
3. **Přímá injekce handlerů + Scrutor decoratory** — + žádná licence, explicitní, jednoduché ladění; − endpoint zná konkrétní typ handleru

## Rozhodnutí

Varianta 3. Decoratory pokryjí validaci i logování a jsou dohledatelné přes „Go to implementation".

## Důsledky

- ✓ Žádná licenční závislost, žádná „magie" v pipeline
- ✗ Notifikace (1 : N) neřeší — pro domain events máme vlastní dispatcher (`event-driven/`)
- Přehodnotit, pokud budeme potřebovat in-process messaging s retry → Wolverine
```

---

## Pravidla pro AI agenta

- Před návrhem změny architektury, nové závislosti nebo odchylky od sady pravidel **přečti `docs/adr/`**.
- Navrhované řešení v rozporu s přijatým ADR → upozorni a navrhni nový ADR, neobcházej ho potichu.
- Když uživatel učiní významné rozhodnutí během práce, **nabídni sepsání ADR** podle šablony.
- Číslo nového ADR = nejvyšší existující + 1; aktualizuj index v `docs/adr/README.md`.
