# Tailwind — Layout & Grid

## Kontejner — standardní vzor

```html
<!-- ✓ Základní kontejner — vždy takto -->
<div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
  <!-- obsah -->
</div>

<!-- ✓ Užší kontejner pro text (články, formuláře) -->
<div class="max-w-3xl mx-auto px-4 sm:px-6">

<!-- ✓ Úzký kontejner pro formuláře, dialogy -->
<div class="max-w-lg mx-auto px-4 sm:px-6">
```

### Přehled max-width hodnot

| Třída | Šířka | Kdy použít |
|-------|-------|-----------|
| `max-w-sm` | 384px | Jednoduchý formulář, card |
| `max-w-md` | 448px | Login formulář, modal |
| `max-w-lg` | 512px | Dialog, bohatší formulář |
| `max-w-xl` | 576px | Sidebar, newsletter |
| `max-w-2xl` | 672px | Blog post, dokumentace |
| `max-w-3xl` | 768px | Článek s obrázky |
| `max-w-4xl` | 896px | Dashboard sekce |
| `max-w-5xl` | 1024px | Stránka s obsahem |
| `max-w-6xl` | 1152px | Wider layout |
| `max-w-7xl` | 1280px | Hlavní kontejner aplikace |

---

## Grid — počty sloupců podle obsahu

### Karty (produkty, příspěvky, uživatelé)

```html
<!-- Karty s hodně textem — max 3 sloupce -->
<div class="grid grid-cols-1 gap-6
            sm:grid-cols-2
            lg:grid-cols-3">
  <div class="card">...</div>
</div>

<!-- Malé karty (náhledy, ikony) — až 4-5 sloupců -->
<div class="grid grid-cols-2 gap-4
            sm:grid-cols-3
            md:grid-cols-4
            xl:grid-cols-5">

<!-- E-shop produkty — zlatý standard -->
<div class="grid grid-cols-1 gap-6
            sm:grid-cols-2
            md:grid-cols-3
            xl:grid-cols-4">
```

### Sidebar layout

```html
<!-- Sidebar vlevo — 1/4 sidebar, 3/4 obsah -->
<div class="flex flex-col gap-8
            lg:flex-row">
  <aside class="w-full lg:w-64 lg:flex-shrink-0">
    ...
  </aside>
  <main class="flex-1 min-w-0">  <!-- min-w-0 zabrání overflow -->
    ...
  </main>
</div>

<!-- Sidebar vpravo — 2/3 obsah, 1/3 sidebar -->
<div class="grid grid-cols-1 gap-8
            lg:grid-cols-3">
  <main class="lg:col-span-2">...</main>
  <aside class="lg:col-span-1">...</aside>
</div>
```

### Dashboard — asymetrické gridy

```html
<!-- Hero karta velká, ostatní malé -->
<div class="grid grid-cols-1 gap-6
            sm:grid-cols-2
            lg:grid-cols-4">
  <div class="sm:col-span-2 lg:col-span-2 row-span-2">
    <!-- Hlavní statistika -->
  </div>
  <div><!-- stat --></div>
  <div><!-- stat --></div>
  <div><!-- stat --></div>
  <div><!-- stat --></div>
</div>
```

### Automatický responzivní grid (bez breakpointů)

```html
<!-- auto-fit — karty se samy zabalí podle prostoru -->
<div class="grid gap-6"
     style="grid-template-columns: repeat(auto-fit, minmax(min(280px, 100%), 1fr))">
  <!-- neomezený počet karet, vždy aspoň 280px -->
</div>
```

> Tailwind nemá utilitu pro `auto-fit` + `minmax` — použij inline style nebo vlastní třídu v CSS.

---

## Gap — mezery mezi prvky

```html
<!-- Standardní mezery pro různé typy gridů -->
<div class="grid gap-4">          <!-- 16px — husté gridy, galerie -->
<div class="grid gap-6">          <!-- 24px — karty, standardní -->
<div class="grid gap-8">          <!-- 32px — sekce, velké karty -->
<div class="grid gap-10 lg:gap-16"> <!-- větší mezery na desktopu -->

<!-- Různé mezery pro osy -->
<div class="grid gap-x-6 gap-y-10"> <!-- užší horizontálně, větší vertikálně -->
```

---

## Padding sekcí — vertikální rytmus

```html
<!-- Sekce stránky -->
<section class="py-12 md:py-16 lg:py-24">

<!-- Hero sekce -->
<section class="py-16 md:py-24 lg:py-32">

<!-- Kompaktní sekce (v kartách, panelech) -->
<div class="p-4 sm:p-6">

<!-- Card padding -->
<div class="p-4 sm:p-6 lg:p-8">
```

---

## Aspect ratio — pro konzistentní karty

```html
<!-- Produktové obrázky — 4:3 -->
<div class="aspect-[4/3] overflow-hidden rounded-lg">
  <img class="w-full h-full object-cover">
</div>

<!-- Hero obrázky — 16:9 -->
<div class="aspect-video overflow-hidden">
  <img class="w-full h-full object-cover">
</div>

<!-- Profilové fotky — čtverec -->
<div class="aspect-square overflow-hidden rounded-full">
  <img class="w-full h-full object-cover">
</div>

<!-- Bannery — wide -->
<div class="aspect-[21/9] sm:aspect-[3/1] overflow-hidden">
  <img class="w-full h-full object-cover object-center">
</div>
```
