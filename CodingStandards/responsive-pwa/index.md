# Responzivní design & PWA — ASP.NET Core (Razor Pages / MVC + Vanilla JS)

Pravidla pro tvorbu responzivních webů a Progressive Web Apps.
Platí pro veškerý HTML, CSS a frontend JS kód.

## Moduly

@mobile-first.md
@grid-layout.md
@pwa-core.md
@service-worker.md
@ui-components.md
@forms.md
@performance-ux.md

## Rychlá kontrola

- [ ] Viewport meta tag je přítomen v každém `<head>`
- [ ] Design vychází z mobilního pohledu — breakpointy rozšiřují, nepřepisují
- [ ] Žádné fixní px šířky na kontejnerech — jen `max-width` + `width: 100%`
- [ ] Obrázky mají `width`, `height` a `loading="lazy"` (mimo LCP)
- [ ] Touch targety mají min. 44×44 px
- [ ] Web manifest je přítomen a odkazován z `<head>`
- [ ] Service Worker je registrován a zvládá offline stav
- [ ] Formuláře mají správné `type`, `autocomplete` a `inputmode`
- [ ] Navigace je ovladatelná klávesnicí i na mobilu
- [ ] Lighthouse PWA score ≥ 90
