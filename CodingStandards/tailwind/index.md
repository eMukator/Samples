# Tailwind CSS — Responzivní design

Pravidla pro používání Tailwind CSS v ASP.NET Core (Razor Pages / MVC + Vanilla JS).
Platí pro Tailwind v3 i v4.

## Moduly

@tw-breakpoints.md
@tw-layout-grid.md
@tw-typography.md
@tw-images.md
@tw-components.md
@tw-config.md

## Rychlá kontrola

- [ ] Třídy psány mobile-first — základní třída bez prefixu, rozšíření s `md:`, `lg:`
- [ ] Žádné magické hodnoty v hranatých závorkách bez zdůvodnění (`w-[347px]`)
- [ ] Kontejner má `max-w-*` + `mx-auto` + `px-4`
- [ ] Obrázky mají `w-full h-auto` nebo `aspect-*` + `object-cover`
- [ ] Interaktivní prvky mají `min-h-[44px] min-w-[44px]`
- [ ] Animace obaleny `motion-safe:` prefixem
- [ ] Dark mode třídy použity konzistentně (`dark:`)
- [ ] Vlastní hodnoty definovány v `tailwind.config` — ne inline
