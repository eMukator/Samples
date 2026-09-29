# Tailwind — Typografie

## Velikosti písma — kdy použít co

| Třída | Velikost | Použití |
|-------|----------|---------|
| `text-xs` | 12px | Metainfo, badge, caption, timestamp |
| `text-sm` | 14px | Popisky, pomocný text, label formuláře |
| `text-base` | 16px | **Základní text těla** — nikdy menší |
| `text-lg` | 18px | Perex, zvýrazněný odstavec |
| `text-xl` | 20px | H4, název karty |
| `text-2xl` | 24px | H3, sekční nadpis |
| `text-3xl` | 30px | H2 na mobilu |
| `text-4xl` | 36px | H2 na desktopu, H1 na mobilu |
| `text-5xl` | 48px | H1 na tabletu |
| `text-6xl` | 60px | H1 na desktopu |
| `text-7xl`+ | 72px+ | Hero nadpisy |

## Nadpisová hierarchie — responzivní vzor

```html
<!-- H1 — hero nadpis -->
<h1 class="text-3xl font-bold tracking-tight
           sm:text-4xl
           lg:text-5xl xl:text-6xl">
  Hlavní nadpis stránky
</h1>

<!-- H2 — sekční nadpis -->
<h2 class="text-2xl font-semibold tracking-tight
           lg:text-3xl">
  Název sekce
</h2>

<!-- H3 — subsekce / název karty -->
<h3 class="text-xl font-semibold
           lg:text-2xl">
  Název subsekce
</h3>

<!-- H4 — nejmenší nadpis -->
<h4 class="text-lg font-medium">
  Název položky
</h4>
```

## Tělo textu — čitelnost

```html
<!-- Standardní odstavec -->
<p class="text-base leading-7 text-gray-700 dark:text-gray-300">

<!-- Perex / první odstavec -->
<p class="text-lg leading-8 text-gray-600 dark:text-gray-400">

<!-- Malý popis pod nadpisem -->
<p class="text-sm leading-6 text-gray-500 dark:text-gray-400">

<!-- Maximální šířka pro čitelnost — nikdy nech text přes celou šířku -->
<div class="max-w-prose">  <!-- = 65ch — optimální délka řádku -->
  <p class="text-base leading-7">Dlouhý článkový text...</p>
</div>
```

## Typografický plugin — `@tailwindcss/typography`

Pro renderování Markdown / HTML obsahu z databáze nebo CMS:

```html
<!-- ✓ Použij prose třídy pro bohatý obsah -->
<article class="prose prose-gray
                prose-sm sm:prose-base lg:prose-lg
                dark:prose-invert
                max-w-none">
  @Html.Raw(Model.Content)
</article>

<!-- Vlastní barvy pro prose -->
<article class="prose prose-blue dark:prose-invert">
```

### Prose velikosti

| Třída | Základní font | Kdy |
|-------|--------------|-----|
| `prose-sm` | 14px | Poznámky, vedlejší obsah |
| `prose-base` | 16px | Standardní články |
| `prose-lg` | 18px | Čtivé dlouhé texty |
| `prose-xl` | 20px | Landing page obsah |
| `prose-2xl` | 24px | Velké hero texty |

## Délka řádku — nikdy přes celou šířku

```html
<!-- ✗ Text přes celou šířku — nečitelné -->
<p class="text-base">Dlouhý text přes 100% šířku kontejneru...</p>

<!-- ✓ Omez délku řádku -->
<p class="text-base max-w-prose">Optimální délka řádku (65 znaků)</p>
<p class="text-base max-w-2xl">Nebo fixní max-width</p>
```

## Zarovnání — pravidla

```html
<!-- ✓ Vlevo — pro tělo textu vždy -->
<p class="text-left">

<!-- ✓ Střed — jen pro krátké texty (hero, CTA, nadpisy sekcí) -->
<h2 class="text-center">

<!-- ✗ Justify — nikdy (nerovnoměrné mezery jsou špatné pro čitelnost) -->
<p class="text-justify">  <!-- NEPOUŽÍVAT -->

<!-- Zarovnání podle breakpointu — center na mobilu, left na desktopu -->
<p class="text-center md:text-left">
```

## Řádkování

```html
<!-- Nadpisy — tight -->
<h1 class="leading-tight">   <!-- 1.25 -->
<h2 class="leading-snug">    <!-- 1.375 -->

<!-- Tělo textu — normal nebo relaxed -->
<p class="leading-normal">   <!-- 1.5 — minimum pro body text -->
<p class="leading-relaxed">  <!-- 1.625 — pohodlné čtení -->
<p class="leading-loose">    <!-- 2.0 — vzdušné, UI labels -->
```
