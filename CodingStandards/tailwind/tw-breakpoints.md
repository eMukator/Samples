# Tailwind — Breakpointy

## Výchozí breakpointy Tailwindu

| Prefix | Min-width | Typické zařízení |
|--------|-----------|-----------------|
| *(none)* | 0px | Mobilní telefon (portrait) |
| `sm:` | 640px | Velký telefon / malý tablet |
| `md:` | 768px | Tablet (portrait) |
| `lg:` | 1024px | Tablet (landscape) / malý laptop |
| `xl:` | 1280px | Desktop |
| `2xl:` | 1536px | Velký desktop |

## Pravidlo mobile-first — psát v tomto pořadí

```html
<!-- ✓ Správně — základ pro mobil, rozšíření pro větší -->
<div class="block md:flex lg:grid lg:grid-cols-3">

<!-- ✗ Špatně — přepisování desktop stylu pro mobil -->
<!-- (Tailwind nemá max-width varianty standardně) -->
```

## Kdy použít který breakpoint

### Žádný prefix (< 640px) — telefon
Základní layout. Vždy single-column, stack vertikálně.

```html
<div class="flex flex-col gap-4 p-4">
```

### `sm:` (640px+) — velký telefon na šířku, mini tablet
Přidej druhý sloupec pro malé karty. Větší padding.

```html
<div class="flex flex-col gap-4 p-4
            sm:flex-row sm:p-6">
```

### `md:` (768px+) — tablet
Sidebar se objeví. Navigace přechází z hamburgeru na horizontální. Dvousloupcový layout.

```html
<div class="grid grid-cols-1 gap-6
            md:grid-cols-2 md:gap-8">
```

### `lg:` (1024px+) — laptop
Tří- nebo čtyřsloupcové gridy. Sticky sidebar. Větší typografie.

```html
<div class="grid grid-cols-1 gap-6
            md:grid-cols-2
            lg:grid-cols-3 lg:gap-10">
```

### `xl:` (1280px+) — desktop
Maximální šířka kontejneru. Čtyř- až pětisloupcové gridy.

```html
<div class="grid grid-cols-2
            lg:grid-cols-3
            xl:grid-cols-4">
```

### `2xl:` (1536px+) — velký monitor
Používej střídmě — jen pro přidání mezery nebo páté karty. Většina layoutů nepotřebuje.

```html
<!-- Příklad kdy 2xl dává smysl -->
<div class="grid grid-cols-2 md:grid-cols-3 xl:grid-cols-4 2xl:grid-cols-5">
```

## Vlastní breakpointy — kdy a jak

Výchozí breakpointy pokrývají 95 % případů. Vlastní přidávej pouze pro specifické potřeby projektu.

```javascript
// tailwind.config.js — Tailwind v3
module.exports = {
  theme: {
    screens: {
      // ✓ Zachovej výchozí a přidej vlastní
      'xs':  '480px',   // velmi malé telefony (volitelné)
      'sm':  '640px',
      'md':  '768px',
      'lg':  '1024px',
      'xl':  '1280px',
      '2xl': '1536px',
      // Maximální šířky — pro specifické komponenty
      'max-md': { max: '767px' },  // použij střídmě
    }
  }
}
```

```css
/* Tailwind v4 — tailwind.config.css */
@import "tailwindcss";

@theme {
  --breakpoint-xs: 480px;
  /* výchozí breakpointy jsou automaticky dostupné */
}
```

## Kombinace breakpointů — praktické vzory

```html
<!-- Navigace: hamburger → horizontální -->
<nav class="flex items-center justify-between
            md:justify-start md:gap-8">

  <button class="md:hidden" aria-label="Menu">...</button>

  <ul class="hidden md:flex gap-6">
    <li><a href="/">Domů</a></li>
  </ul>
</nav>

<!-- Pořadí prvků — mobil: obrázek nahoře, desktop: vedle textu -->
<article class="flex flex-col gap-6
                lg:flex-row lg:items-start">
  <img class="w-full lg:w-64 lg:flex-shrink-0">
  <div class="flex-1">...</div>
</article>

<!-- Skrývání prvků — jen pro konkrétní breakpointy -->
<div class="hidden sm:block">Viditelné od sm</div>
<div class="sm:hidden">Pouze na mobilu</div>
<div class="hidden md:block xl:hidden">Jen tablet</div>
```
