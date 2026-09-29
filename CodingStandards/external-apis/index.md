# Externí API — reference

Dokumentace a limity externích služeb používaných v projektech.

---

## Mapy.com Tile API

**Dokumentace:**
- Přehled (llms-friendly): https://github.com/mapycom/developer/blob/master/llms.txt
- Maptiles spec: https://github.com/mapycom/developer/blob/master/docs/rest-api/map-tiles.md

**Tile URL formát:**
```
https://api.mapy.com/v1/maptiles/{mapset}/{tileSize}/{z}/{x}/{y}?apikey={KEY}&lang={LANG}
```

**Parametry:**

| Parametr | Povinný | Hodnoty |
|---|---|---|
| `mapset` | ano | `basic`, `outdoor`, `winter`, `aerial`, `names-overlay` |
| `tileSize` | ano | `256`, `256@2x` (retina jen pro basic a outdoor) |
| `z` / `x` / `y` | ano | souřadnice dlaždice (zoom 0–20) |
| `apikey` | ano | API klíč (alternativně hlavička `X-Mapy-Api-Key`) |
| `lang` | ne | viz níže; výchozí `cs`; platí **jen pro zoom ≤ 6** |

**Podporované hodnoty `lang`:**
`cs`, `de`, `el`, `en`, `es`, `fr`, `it`, `nl`, `pl`, `pt`, `ru`, `sk`, `tr`, `uk`

> Pozor: `hu` (maďarština) **není** podporováno → API vrátí 404. Použij fallback `en`.
> Pro zoom > 6 nemá `lang` žádný efekt; popisky jsou renderovány přímo v dlaždicích v češtině.
> Výjimka: `cs` a `sk` zobrazí názvy států v daném jazyce i na nízkých zoomech; ostatní jazyky zobrazí anglické názvy států.

**Rate limit:** 500 požadavků za sekundu na API klíč.

**Logo (povinné):** Nad mapou musí být zobrazeno logo Mapy.cz:
```html
<a href="https://mapy.cz" target="_blank">
  <img src="https://api.mapy.com/img/api/logo.svg" />
</a>
```
