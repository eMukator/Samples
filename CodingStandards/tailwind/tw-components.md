# Tailwind — Komponenty & Vzory

## Tlačítka

```html
<!-- Primární -->
<button class="inline-flex items-center justify-center gap-2
               px-4 py-2.5 min-h-[44px]
               bg-blue-600 hover:bg-blue-700 active:bg-blue-800
               text-white text-sm font-medium
               rounded-lg
               focus-visible:outline focus-visible:outline-2
               focus-visible:outline-offset-2 focus-visible:outline-blue-600
               disabled:opacity-50 disabled:cursor-not-allowed
               transition-colors duration-150">
  Odeslat objednávku
</button>

<!-- Sekundární (outline) -->
<button class="inline-flex items-center justify-center gap-2
               px-4 py-2.5 min-h-[44px]
               border border-gray-300 dark:border-gray-600
               bg-white dark:bg-gray-800
               hover:bg-gray-50 dark:hover:bg-gray-700
               text-gray-700 dark:text-gray-300 text-sm font-medium
               rounded-lg
               focus-visible:outline focus-visible:outline-2
               focus-visible:outline-offset-2 focus-visible:outline-blue-600
               transition-colors duration-150">
  Zrušit
</button>

<!-- Full-width na mobilu, auto na desktopu -->
<button class="w-full sm:w-auto inline-flex items-center justify-center
               px-6 py-3 min-h-[44px] ...">
  Pokračovat
</button>

<!-- Tlačítko s ikonou -->
<button class="inline-flex items-center gap-2 px-4 py-2.5 min-h-[44px] ...">
  <svg class="w-4 h-4" aria-hidden="true">...</svg>
  Přidat do košíku
</button>

<!-- Icon-only tlačítko -->
<button class="flex items-center justify-center
               w-11 h-11 rounded-lg
               text-gray-500 hover:text-gray-700
               hover:bg-gray-100 dark:hover:bg-gray-700
               focus-visible:outline focus-visible:outline-2
               focus-visible:outline-blue-600"
        aria-label="Smazat položku">
  <svg class="w-5 h-5" aria-hidden="true">...</svg>
</button>
```

## Karty

```html
<!-- Základní karta -->
<div class="bg-white dark:bg-gray-800
            rounded-xl border border-gray-200 dark:border-gray-700
            shadow-sm hover:shadow-md
            transition-shadow duration-200
            overflow-hidden">

  <!-- Obrázek karty -->
  <div class="aspect-[16/9] overflow-hidden">
    <img src="..." alt="..."
         class="w-full h-full object-cover
                hover:scale-105 motion-safe:duration-300">
  </div>

  <!-- Obsah karty -->
  <div class="p-4 sm:p-6 flex flex-col gap-3">
    <div class="flex items-center gap-2 text-xs text-gray-500 uppercase tracking-wide">
      <span>Kategorie</span>
      <span>·</span>
      <time datetime="2025-01-01">1. ledna 2025</time>
    </div>

    <h3 class="text-lg font-semibold text-gray-900 dark:text-white
               line-clamp-2">
      Název karty
    </h3>

    <p class="text-sm text-gray-600 dark:text-gray-400 leading-relaxed
              line-clamp-3">
      Popis karty...
    </p>

    <div class="mt-auto pt-3 flex items-center justify-between">
      <span class="text-xl font-bold text-gray-900 dark:text-white">
        1 299 Kč
      </span>
      <a href="#" class="btn-primary text-sm px-3 py-2">Koupit</a>
    </div>
  </div>
</div>
```

## Formulářové pole

```html
<div class="flex flex-col gap-1.5">
  <label for="email"
         class="text-sm font-medium text-gray-700 dark:text-gray-300">
    E-mailová adresa
    <span class="text-red-500 ml-0.5" aria-hidden="true">*</span>
  </label>

  <input id="email" name="email" type="email"
         inputmode="email" autocomplete="email"
         placeholder="jan@example.com"
         class="w-full px-3.5 py-2.5 min-h-[44px]
                text-base text-gray-900 dark:text-white
                bg-white dark:bg-gray-800
                border border-gray-300 dark:border-gray-600
                rounded-lg
                placeholder:text-gray-400
                focus:outline-none focus:ring-2 focus:ring-blue-500
                focus:border-transparent
                disabled:bg-gray-50 disabled:cursor-not-allowed
                aria-invalid:border-red-500 aria-invalid:ring-red-200">

  <!-- Chybová zpráva -->
  <p id="email-error"
     class="text-sm text-red-600 dark:text-red-400 hidden"
     role="alert">
    Zadejte platnou e-mailovou adresu.
  </p>

  <!-- Nápověda -->
  <p class="text-xs text-gray-500 dark:text-gray-400">
    Na tento e-mail pošleme potvrzení.
  </p>
</div>
```

## Badge / Tag

```html
<!-- Stavové badge -->
<span class="inline-flex items-center gap-1 px-2.5 py-0.5
             text-xs font-medium rounded-full
             bg-green-100 text-green-800
             dark:bg-green-900/30 dark:text-green-400">
  <span class="w-1.5 h-1.5 rounded-full bg-green-500" aria-hidden="true"></span>
  Aktivní
</span>

<!-- Škála barev pro stavy -->
<!-- Zelená: úspěch, aktivní, k dispozici -->
<!-- Žlutá: varování, čekající, probíhá -->
<!-- Červená: chyba, zrušeno, nebezpečí -->
<!-- Modrá: info, nové, označeno -->
<!-- Šedá: neaktivní, archivováno -->
```

## Alert / Notification box

```html
<div class="flex gap-3 p-4 rounded-lg
            bg-blue-50 dark:bg-blue-900/20
            border border-blue-200 dark:border-blue-800"
     role="alert">
  <svg class="w-5 h-5 text-blue-500 flex-shrink-0 mt-0.5" aria-hidden="true">...</svg>
  <div class="flex-1 min-w-0">
    <p class="text-sm font-medium text-blue-800 dark:text-blue-300">
      Nadpis upozornění
    </p>
    <p class="mt-1 text-sm text-blue-700 dark:text-blue-400">
      Text upozornění...
    </p>
  </div>
</div>
```

## Prázdný stav (Empty state)

```html
<div class="flex flex-col items-center justify-center
            py-12 sm:py-16 text-center
            max-w-sm mx-auto">
  <div class="w-16 h-16 rounded-full bg-gray-100 dark:bg-gray-800
              flex items-center justify-center mb-4">
    <svg class="w-8 h-8 text-gray-400" aria-hidden="true">...</svg>
  </div>
  <h3 class="text-base font-semibold text-gray-900 dark:text-white mb-1">
    Žádné objednávky
  </h3>
  <p class="text-sm text-gray-500 dark:text-gray-400 mb-6">
    Zatím nemáte žádné objednávky. Začněte přidáním první.
  </p>
  <a href="/orders/new"
     class="inline-flex items-center gap-2 px-4 py-2.5 min-h-[44px]
            bg-blue-600 text-white text-sm font-medium rounded-lg
            hover:bg-blue-700 transition-colors">
    Vytvořit objednávku
  </a>
</div>
```

## Zakázané vzory

```html
<!-- ✗ Arbitrary values bez důvodu — použij nejbližší design token -->
<div class="w-[347px] mt-[23px] text-[13px]">

<!-- ✓ Zaokrouhlená hodnota z Tailwind scale -->
<div class="w-80 mt-6 text-sm">

<!-- ✗ Inline style místo Tailwind -->
<div style="margin-top: 24px; color: #374151">

<!-- ✗ Duplikace tříd pro každý prvek — extrahuj do CSS třídy nebo komponentu -->
<button class="px-4 py-2.5 bg-blue-600 text-white rounded-lg ...">Btn 1</button>
<button class="px-4 py-2.5 bg-blue-600 text-white rounded-lg ...">Btn 2</button>
<button class="px-4 py-2.5 bg-blue-600 text-white rounded-lg ...">Btn 3</button>

<!-- ✓ Extrahuj do @apply nebo Razor partial -->
```
