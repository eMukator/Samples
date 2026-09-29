# Tailwind — Konfigurace & Projekt setup

## tailwind.config.js — doporučená konfigurace (v3)

```javascript
/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './Pages/**/*.cshtml',
    './Views/**/*.cshtml',
    './wwwroot/js/**/*.js',
  ],

  darkMode: 'class',  // nebo 'media' pro automatický dark mode

  theme: {
    extend: {
      // ✓ Vlastní barvy — definuj jako CSS proměnné pro dark mode kompatibilitu
      colors: {
        primary: {
          50:  'rgb(var(--color-primary-50) / <alpha-value>)',
          100: 'rgb(var(--color-primary-100) / <alpha-value>)',
          500: 'rgb(var(--color-primary-500) / <alpha-value>)',
          600: 'rgb(var(--color-primary-600) / <alpha-value>)',
          700: 'rgb(var(--color-primary-700) / <alpha-value>)',
        },
      },

      // ✓ Vlastní font family
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
        mono: ['JetBrains Mono', 'monospace'],
      },

      // ✓ Vlastní animace
      animation: {
        'fade-in':    'fade-in 200ms ease-out',
        'slide-up':   'slide-up 300ms ease-out',
        'shimmer':    'shimmer 1.5s infinite',
      },

      keyframes: {
        'fade-in': {
          from: { opacity: '0' },
          to:   { opacity: '1' },
        },
        'slide-up': {
          from: { opacity: '0', transform: 'translateY(0.5rem)' },
          to:   { opacity: '1', transform: 'translateY(0)' },
        },
        'shimmer': {
          '0%':   { backgroundPosition: '200% 0' },
          '100%': { backgroundPosition: '-200% 0' },
        },
      },

      // ✓ Vlastní max-width pro kontejnery
      maxWidth: {
        'prose-narrow': '55ch',
        'prose-wide':   '75ch',
        'screen-2xl':   '1536px',
      },
    },
  },

  plugins: [
    require('@tailwindcss/typography'),   // prose třídy pro bohatý text
    require('@tailwindcss/forms'),         // reset formulářů
    require('@tailwindcss/aspect-ratio'), // aspect-ratio (v3, ve v4 nativní)
  ],
}
```

## Tailwind v4 — CSS konfigurace

```css
/* wwwroot/css/tailwind.css */
@import "tailwindcss";

@theme {
  /* Vlastní barvy */
  --color-primary-500: oklch(0.5 0.2 250);
  --color-primary-600: oklch(0.45 0.22 250);

  /* Vlastní fonty */
  --font-sans: "Inter", system-ui, sans-serif;

  /* Vlastní breakpoint */
  --breakpoint-xs: 480px;
}
```

## CSS soubor — globální styly a @apply

```css
/* wwwroot/css/site.css */
@tailwind base;
@tailwind components;
@tailwind utilities;

/* ✓ @apply pouze pro opakující se vzory (tlačítka, formuláře) */
@layer components {
  .btn {
    @apply inline-flex items-center justify-center gap-2
           px-4 py-2.5 min-h-[44px]
           text-sm font-medium rounded-lg
           focus-visible:outline focus-visible:outline-2
           focus-visible:outline-offset-2
           disabled:opacity-50 disabled:cursor-not-allowed
           transition-colors duration-150;
  }

  .btn-primary {
    @apply btn bg-blue-600 text-white
           hover:bg-blue-700 active:bg-blue-800
           focus-visible:outline-blue-600;
  }

  .btn-secondary {
    @apply btn border border-gray-300 dark:border-gray-600
           bg-white dark:bg-gray-800
           text-gray-700 dark:text-gray-300
           hover:bg-gray-50 dark:hover:bg-gray-700
           focus-visible:outline-blue-600;
  }

  .form-input {
    @apply w-full px-3.5 py-2.5 min-h-[44px]
           text-base text-gray-900 dark:text-white
           bg-white dark:bg-gray-800
           border border-gray-300 dark:border-gray-600
           rounded-lg placeholder:text-gray-400
           focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent;
  }
}

/* ✓ Vlastní utility pro věci které Tailwind nemá */
@layer utilities {
  .scrollbar-hide {
    scrollbar-width: none;
    &::-webkit-scrollbar { display: none; }
  }

  .text-balance {
    text-wrap: balance;  /* zabrání "osamoceným" slovům na posledním řádku */
  }
}

/* ✓ CSS custom properties pro témování */
@layer base {
  :root {
    --color-primary-500: 59 130 246;   /* blue-500 */
    --color-primary-600: 37 99 235;    /* blue-600 */
  }

  .dark {
    --color-primary-500: 96 165 250;   /* blue-400 — světlejší pro dark mode */
    --color-primary-600: 59 130 246;
  }
}
```

## Razor Pages — jak organizovat třídy

```cshtml
@* ✓ Dlouhé třídy zalamuj na více řádků pro čitelnost *@
<div class="grid grid-cols-1 gap-6
            sm:grid-cols-2
            lg:grid-cols-3 lg:gap-8
            xl:grid-cols-4">

@* ✓ Podmíněné třídy v Razor *@
<div class="px-4 py-2 rounded-full text-sm font-medium
            @(Model.IsActive
              ? "bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400"
              : "bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400")">
  @(Model.IsActive ? "Aktivní" : "Neaktivní")
</div>

@* ✓ Pomocná metoda pro opakující se podmíněné třídy *@
@{
    string StatusClass(OrderStatus status) => status switch
    {
        OrderStatus.Pending   => "bg-yellow-100 text-yellow-800",
        OrderStatus.Confirmed => "bg-blue-100 text-blue-800",
        OrderStatus.Shipped   => "bg-purple-100 text-purple-800",
        OrderStatus.Delivered => "bg-green-100 text-green-800",
        OrderStatus.Cancelled => "bg-red-100 text-red-800",
        _ => "bg-gray-100 text-gray-800"
    };
}

<span class="inline-flex px-2.5 py-0.5 rounded-full text-xs font-medium
             @StatusClass(Model.Status)">
  @Model.Status
</span>
```

## Purge / Content — časté chyby

```javascript
// ✗ Dynamicky sestavené třídy — Tailwind je nenajde při purge
const color = 'blue';
<div class={`text-${color}-600`}>  // text-blue-600 bude odstraněno!

// ✓ Celé názvy tříd v safelist nebo přímo v HTML
const classes = {
  blue: 'text-blue-600',
  red: 'text-red-600',
};
<div class={classes[color]}>

// ✓ Nebo přidej do safelist v config
safelist: [
  { pattern: /text-(blue|red|green|yellow)-(600|700)/ }
]
```
