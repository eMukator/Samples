# Tailwind — Obrázky

## Základní pravidla

```html
<!-- ✓ Obrázek vždy přizpůsobený kontejneru -->
<img src="..." alt="Popis" width="800" height="600"
     class="w-full h-auto">

<!-- ✓ LCP obrázek (hero, první velký obrázek) — bez lazy -->
<img src="..." alt="Hero" width="1200" height="600"
     class="w-full h-auto"
     fetchpriority="high">

<!-- ✓ Všechny ostatní — lazy loading -->
<img src="..." alt="..." width="400" height="300"
     class="w-full h-auto"
     loading="lazy">
```

## Velikosti obrázků — doporučení

### Hero / Banner obrázky

```html
<!-- Plná šířka hero -->
<div class="relative w-full aspect-[16/9] sm:aspect-[3/1] lg:aspect-[21/9]">
  <img src="/img/hero-1200.webp"
       srcset="/img/hero-640.webp 640w,
               /img/hero-1024.webp 1024w,
               /img/hero-1920.webp 1920w"
       sizes="100vw"
       alt="Hero popis"
       class="w-full h-full object-cover object-center"
       fetchpriority="high">
</div>

<!-- Doporučené rozlišení zdrojových souborů:
  Mobile:  640×360px  (< 50 KB WebP)
  Tablet:  1024×576px (< 100 KB WebP)
  Desktop: 1920×1080px (< 200 KB WebP) -->
```

### Produktové / kartové obrázky

```html
<!-- Pevný aspect ratio — všechny karty stejně vysoké -->
<div class="aspect-[4/3] overflow-hidden rounded-lg bg-gray-100">
  <img src="/img/product-400.webp"
       srcset="/img/product-400.webp 400w,
               /img/product-800.webp 800w"
       sizes="(max-width: 640px) calc(100vw - 2rem),
              (max-width: 1024px) calc(50vw - 2rem),
              calc(33vw - 2rem)"
       alt="Název produktu"
       class="w-full h-full object-cover transition-transform
              hover:scale-105 motion-safe:duration-300"
       loading="lazy">
</div>

<!-- Doporučené rozlišení:
  Thumbnail: 400×300px  (< 30 KB WebP)
  Standard:  800×600px  (< 60 KB WebP) -->
```

### Profilové fotky / avatary

```html
<!-- Malý avatar (v navigaci, komentářích) -->
<img src="/img/avatar.webp"
     alt="Jméno uživatele"
     width="40" height="40"
     class="w-10 h-10 rounded-full object-cover flex-shrink-0"
     loading="lazy">

<!-- Střední avatar (profil, karty) -->
<img src="/img/avatar.webp"
     alt="Jméno uživatele"
     width="80" height="80"
     class="w-16 h-16 sm:w-20 sm:h-20 rounded-full object-cover"
     loading="lazy">

<!-- Velký avatar (stránka profilu) -->
<img src="/img/avatar.webp"
     alt="Jméno uživatele"
     width="160" height="160"
     class="w-24 h-24 sm:w-32 sm:h-32 lg:w-40 lg:h-40 rounded-full object-cover">
```

### Galerie obrázků

```html
<!-- Fotogalerie — různé formáty -->
<div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-2 sm:gap-3">
  <div class="aspect-square overflow-hidden rounded-md">
    <img src="..." alt="..." class="w-full h-full object-cover
                                    hover:scale-105 motion-safe:duration-300
                                    cursor-pointer"
         loading="lazy">
  </div>
</div>

<!-- Pinterest-style masonry (CSS columns) -->
<div class="columns-2 sm:columns-3 lg:columns-4 gap-3">
  <div class="break-inside-avoid mb-3">
    <img src="..." alt="..." class="w-full h-auto rounded-md" loading="lazy">
  </div>
</div>
```

### Blog / článkové obrázky

```html
<!-- Thumbnail v listingu článků -->
<div class="aspect-[16/9] overflow-hidden rounded-xl">
  <img src="..."
       alt="Popis obrázku článku"
       width="800" height="450"
       class="w-full h-full object-cover"
       loading="lazy">
</div>

<!-- Inline obrázek v textu článku -->
<figure class="my-8">
  <img src="..."
       alt="Popis"
       width="800" height="500"
       class="w-full h-auto rounded-xl"
       loading="lazy">
  <figcaption class="mt-2 text-sm text-center text-gray-500 dark:text-gray-400">
    Popis obrázku
  </figcaption>
</figure>
```

## Placeholder při načítání

```html
<!-- Blur placeholder — zobraz nízkorozlišenou verzi dokud se načte plná -->
<div class="relative overflow-hidden bg-gray-200 dark:bg-gray-700">
  <img src="/img/product-tiny.webp"   <!-- 20px blur placeholder -->
       data-src="/img/product-800.webp"
       alt="Produkt"
       class="w-full h-auto blur-sm scale-110 transition-[filter,transform]
              duration-500 data-loaded:blur-0 data-loaded:scale-100"
       loading="lazy">
</div>

<!-- Fallback skeleton (CSS-only) -->
<div class="w-full aspect-[4/3] bg-gray-200 dark:bg-gray-700
            animate-pulse rounded-lg">
</div>
```

## Ikony — SVG pravidla

```html
<!-- Inline SVG ikona (pro obarvení přes text-*) -->
<svg class="w-5 h-5 text-gray-500 flex-shrink-0"
     aria-hidden="true"
     fill="none" stroke="currentColor" viewBox="0 0 24 24">
  <path ...>
</svg>

<!-- Velikosti ikon -->
<svg class="w-4 h-4">   <!-- 16px — v textu, tabulkách -->
<svg class="w-5 h-5">   <!-- 20px — tlačítka, navigace -->
<svg class="w-6 h-6">   <!-- 24px — standardní UI ikona -->
<svg class="w-8 h-8">   <!-- 32px — feature ikona -->
<svg class="w-12 h-12"> <!-- 48px — velká feature ikona -->
<svg class="w-16 h-16"> <!-- 64px — ilustrace, prázdné stavy -->
```
